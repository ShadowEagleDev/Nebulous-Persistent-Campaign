# Nebulous: Persistent Campaign Save Tools

**Persistent Campaign** allows GM's to run multi-session, persistent campaigns in *Nebulous: Fleet Command*.

**Mod essentially does nothing as is, it's a tool for GM's**

By hooking into the deployment screen, this mod reads an externally made save file and injects prior battle damage, expended ammunition, and destroyed modules directly into skirmish/mp lobbies. You control the data carried over by editing the file as needed. If some data is deleted, the fleet will take .fleet data normally.


## How It Works
This mod is designed to be used alongside a Game Master (GM) running a campaign. 
1. At the end of a match, the mod automatically generates a silent background save to `\Nebulous\Saves\Skirmish`.
2. The GM processes this save as needed and generates a master `PersistentFleet.save` file.
3. The GM distributes this file to all players before the next game.
4. When the lobby transitions to the Deployment phase, the mod reads the file and applies the save locally to everyone's ships.

## Installation & Usage
**If you are playing in a campaign, you MUST have this mod enabled, and you MUST have the save file from your GM.**

1. Subscribe to this mod on the Steam Workshop and enable it in-game.
2. Download the `PersistentFleet.save` file provided by your Game Master.
3. Place the file exactly here: 
   `Steam\steamapps\common\Nebulous\Saves\PersistentFleet.save`
4. Join the multiplayer lobby using your `.fleet` file.

## Known Limitations
* **Spacecraft & Hangars:** Due to how the engine assigns network IDs to strike craft in multiplayer lobbies, saved hangar states currently cause conflicts. Carriers will retain their hull/component damage and internal ammo reserves, but the strike craft themselves will NOT spawn if `PersistentFleet.save` file still has "CraftHangarState" blocks populated. These need to be removed for craft to spawn as per `.fleet` file.*
* **Mirror Matches:** Bringing the exact same `.fleet` file as your opponent is supported. The mod specifically targets a combination of your Player Name and Ship key to ensure your battle damage doesn't apply to the enemy's identical ship. A consequence of this is that you NEED to keep the same ship key for mod to function, either by keeping to the same ship in ship editor, or by enforcing it manually *
