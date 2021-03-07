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

Default hot key is LeftAlt + F10.  
Hot key format is as follows.  
https://docs.unity3d.com/Manual/ConventionalGameInput.html

~~~ini
## Settings file was created by plugin Automatic Door Mod v0.0.4
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
