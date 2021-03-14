# AutomaticDoorMod
This is a mod that automatically closes the door that the player has opened after a certain period of time.

## Demonstration
![demo](https://raw.githubusercontent.com/wiki/muro1214/AutomaticDoorMod/images/AutomaticDoor.gif)

## MOD Specs.

* Automatically opens the door when a player approaches.
* After the door is automatically opened, it will automatically close when the player leaves or after a certain period of time.
* In Crypt, the auto-close feature is disabled.
* Use hotkeys to enable and disable the mod.

## Installation
1. Install BepInEx
2. Unzip `AutomaticDoorMod_x.x.x.zip` (x.x.x is version)
3. Copy `AutomaticDoorMod.dll` to `<Valheim Root Dir>\BepInEx\plugins`

## Configuration
Please edit `.\BepInEx\config\muro1214.valheim_mods.automatic_door.cfg`.  

Hot key format is as follows.  
https://docs.unity3d.com/Manual/ConventionalGameInput.html

~~~ini
## Settings file was created by plugin Automatic Door Mod v0.1.0
## Plugin GUID: muro1214.valheim_mods.automatic_door

[General]

## If you set this to false, this mod will be disabled.
# Setting type: Boolean
# Default value: true
IsEnabled = true

## Specify the time in seconds to wait for the door to close automatically.
# Setting type: Single
# Default value: 5
waitForDoorToCloseSeconds = 5

## Doors DO NOT CLOSE automatically when a player is in range.
## If set to 0, the door will automatically close regardless of distance.
# Setting type: Single
# Default value: 4
automaticDoorCloseRange = 4

## When a player is within range, the door will open automatically.
## If set to 0, this feature is disabled.
# Setting type: Single
# Default value: 4
automaticDoorOpenRange = 4

## If set to true, disables the setting that automatically opens the door when you are inside Crypt.
# Setting type: Boolean
# Default value: true
disableAutomaticDoorOpenInCrypt = true

## Specifies the MOD Key of toggleSwitchKey. If left blank, it is not used.
# Setting type: String
# Default value: left alt
toggleSwitchModKey = left alt

## Toggles between enabled and disabled mods when this key is pressed.
# Setting type: String
# Default value: f10
toggleSwitchKey = f10
~~~

## Changelog
* v0.0.1: Initial Release
* v0.0.2: Disable mod when player is in SunkenCrypt
* v0.0.3: Disable mod when player is in Crypt -> support for both Forest Crypt and Sunken Crypt
* v0.0.4: Added hotkeys to enable and disable mod
* v0.0.5: 'Hotfix' Detecting Crypt is not working. Thanks, @turkoid
* v0.1.0: Open the door automatically. The door will not close when the player is in range.

## Planned for implementation
### Next version
* Automatically close the door even if the player teleports away.
