# AutomaticDoorMod
This is a mod that automatically closes the door that the player has opened after a certain period of time.

## Demonstration
![demo](https://raw.githubusercontent.com/wiki/muro1214/AutomaticDoorMod/images/AutomaticDoor.gif)

## Installation
1. Install BepInEx
2. Unzip `AutomaticDoorMod_x.x.x.zip` (x.x.x is version)
3. Copy `AutomaticDoorMod.dll` to `<Valheim Root Dir>\BepInEx\plugins`

## Configuration
Please edit `.\BepInEx\config\muro1214.valheim_mods.automatic_door.cfg`.

~~~ini
## Settings file was created by plugin Automatic Door Mod v0.0.1
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
~~~

## Changelog
* v0.0.1: Initial Release
