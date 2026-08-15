using Game;
using HarmonyLib;
using Modding;
using UnityEngine;
using System;
using System.Xml;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using Utility;
using Ships;
using Ships.SaveGame;
using Game.Units;

namespace PersistentCampaign
{
    public class PersistentCampaignMod : IModEntryPoint
    {
        public void PostLoad()
        {
            Harmony harmony = new Harmony("nebulous.persistent-campaign");
            harmony.PatchAll();
        }

        public void PreLoad() { }
    }

    [HarmonyPatch(typeof(SkirmishGameManager), "TransitionFinished")]
    class Patch_SkirmishGameManager_TransitionFinished
    {
        static bool Prefix(ref SkirmishGameManager __instance)
        {
            bool isHost = Traverse.Create(__instance).Property("IsHost").GetValue<bool>();
            if (!isHost) return true;

            Debug.Log("SAVEFLEETSTATE :: Match over! Executing silent internal save...");

            if (__instance != null)
            {
                var hostInstance = Traverse.Create(__instance).Field("_host").GetValue();

                if (hostInstance != null)
                {
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                    string fileName = "Skirmish_AutoSave_" + timestamp;

                    Traverse.Create(hostInstance).Method("SaveGameInternal", fileName).GetValue();

                    Debug.Log("SAVEFLEETSTATE :: Match saved in the background as: " + fileName);
                }
                else
                {
                    Debug.LogWarning("SAVEFLEETSTATE :: Could not save! The _host was null.");
                }
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(SkirmishGameManager), "TransitionChooseSpawn")]
    class Patch_SkirmishGameManager_TransitionChooseSpawn
    {
        static void Postfix(SkirmishGameManager __instance)
        {
            Debug.Log("INJECTOR :: Deployment Screen reached! Initializing State Injector...");

            FilePath path = new FilePath("PersistentFleet.save", "Saves");
            if (!path.Exists)
            {
                Debug.LogError("INJECTOR :: CRITICAL WARNING! No PersistentFleet.save found on this machine!");
                Debug.LogError("INJECTOR :: If this is a multiplayer match, you will DESYNC from the host! Please put the file in your Saves folder.");
                return;
            }

            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(path.RelativePath);

                Type xmlInterface = typeof(SavedShipState).GetInterface("IXmlDocSerializable");
                if (xmlInterface == null)
                {
                    foreach (Type i in typeof(SavedShipState).GetInterfaces())
                    {
                        if (i.Name.Contains("IXmlDocSerializable"))
                        {
                            xmlInterface = i;
                            break;
                        }
                    }
                }

                MethodInfo readMethod = xmlInterface?.GetMethod("ReadFromDocument");
                IEnumerable players = Traverse.Create(__instance).Property("Players").GetValue<IEnumerable>();

                foreach (object playerObj in players)
                {
                    SkirmishPlayer sPlayer = playerObj as SkirmishPlayer;
                    if (sPlayer == null || sPlayer.PlayerFleet == null) continue;

                    string playerName = Traverse.Create(sPlayer).Property("Name").GetValue<string>();
                    if (string.IsNullOrEmpty(playerName))
                        playerName = Traverse.Create(sPlayer).Property("PlayerName").GetValue<string>();

                    foreach (Ship activeShip in sPlayer.PlayerFleet.FleetShips)
                    {
                        Guid internalKey = Traverse.Create(activeShip).Field("_key").GetValue<Guid>();
                        string shipGuid = internalKey.ToString();

                        string shipName = Traverse.Create(activeShip).Property("ShipName").GetValue<string>();
                        if (string.IsNullOrEmpty(shipName))
                            shipName = Traverse.Create(activeShip).Property("GivenName").GetValue<string>();

                        Debug.Log("INJECTOR :: Processing active ship: " + shipName + " (Key: " + shipGuid + ") for Player: " + playerName);

                        string xPath = "//PlayerInfo[Name='" + playerName + "']//FleetState//Ship[Key='" + shipGuid + "']/Value";
                        XmlNode stateNode = doc.SelectSingleNode(xPath);

                        if (stateNode == null)
                        {
                            stateNode = doc.SelectSingleNode("//PlayerInfo[Name='" + playerName + "']//FleetState//Ship[Key='" + shipGuid + "']");
                        }

                        if (stateNode != null)
                        {
                            Debug.Log("INJECTOR :: Found damage state for " + shipName + ". Injecting...");

                            SavedShipState state = new SavedShipState();

                            if (readMethod != null)
                            {
                                readMethod.Invoke(state, new object[] { stateNode as XmlElement });
                            }
                            else
                            {
                                Debug.LogError("INJECTOR :: Could not resolve ReadFromDocument method.");
                            }

                            ShipController controller = activeShip.gameObject.GetComponent<ShipController>();
                            if (controller != null)
                            {
                                controller.LoadSavedState(state);
                                Debug.Log("INJECTOR :: SUCCESS! Damage applied locally to " + shipName);
                            }
                        }
                        else
                        {
                            Debug.LogWarning("INJECTOR :: " + shipName + " (Key: " + shipGuid + ") not found for player " + playerName + ". Spawning pristine.");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("INJECTOR :: Failed to inject state: " + e.Message + "\n" + e.StackTrace);

                if (e.InnerException != null)
                {
                    Debug.LogError("INJECTOR :: Inner Exception: " + e.InnerException.Message + "\n" + e.InnerException.StackTrace);
                }
            }
        }
    }
}