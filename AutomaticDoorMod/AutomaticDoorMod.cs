using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace AutomaticDoorMod
{
    [BepInPlugin("muro1214.valheim_mods.automatic_door", "Automatic Door Mod", toolVersion)]
    public class AutomaticDoorModPlugin : BaseUnityPlugin
    {
        public const string toolVersion = "0.0.4";

        public static ConfigEntry<bool> isEnabled;
        public static ConfigEntry<float> waitForDoorToCloseSeconds;
        public static ConfigEntry<string> toggleSwitchModKey;
        public static ConfigEntry<string> toggleSwitchKey;

        private void Awake()
        {
            isEnabled = Config.Bind<bool>("General", "IsEnabled", true, "If you set this to false, this mod will be disabled.");
            waitForDoorToCloseSeconds = Config.Bind<float>("General", "waitForDoorToCloseSeconds", 5, "Specify the time in seconds to wait for the door to close automatically.");
            toggleSwitchModKey = Config.Bind<string>("General", "toggleSwitchModKey", "left alt", "Specifies the MOD Key of toggleSwitchKey. If left blank, it is not used.");
            toggleSwitchKey = Config.Bind<string>("General", "toggleSwitchKey", "f10", "Toggles between enabled and disabled mods when this key is pressed.");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        [HarmonyPatch(typeof(Door), "Interact")]
        public static class AutomaticDoor
        {
            public static bool isInsideCrypt = false;
            public static bool toggleSwitch = true;

            private static void Postfix(ref Door __instance, ZNetView ___m_nview)
            {
                if (!AutomaticDoorModPlugin.isEnabled.Value || // when mod is disabled
                    __instance.m_keyItem != null || // when target door needs keyItem (e.g. CryptKey)
                    isInsideCrypt || // when player is in Crypt
                    !toggleSwitch) // when a player manually disables a mod
                {
                    return;
                }

                IEnumerator enumerator = DoorCloseDelay(AutomaticDoorModPlugin.waitForDoorToCloseSeconds.Value, () =>
                {
                    ___m_nview.GetZDO().Set("state", 0);
                });

                ___m_nview.StopAllCoroutines();
                ___m_nview.StartCoroutine(enumerator);
            }

            private static IEnumerator DoorCloseDelay(float wait_time, Action action)
            {
                yield return new WaitForSeconds(wait_time);

                action?.Invoke();
            }
        }

        [HarmonyPatch(typeof(EnvZone), "OnTriggerStay")]
        public static class DisableModWhenCryptStay
        {
            private static string currentEnv = "";

            private static void Prefix(Collider collider, ref EnvZone __instance)
            {
                if (!AutomaticDoorModPlugin.isEnabled.Value)
                {
                    return;
                }

                if(currentEnv == __instance.m_environment)
                {
                    return;
                }

                Player component = collider.GetComponent<Player>();
                if(component == null || Player.m_localPlayer != component)
                {
                    return;
                }
                
                if (__instance.m_environment.Contains("Crypt"))
                {
                    AutomaticDoor.isInsideCrypt = true;
                    currentEnv = __instance.m_environment;
                }
            }
        }

        [HarmonyPatch(typeof(EnvZone), "OnTriggerExit")]
        public static class EnableModWhenCryptExit
        {
            private static void Prefix(Collider collider, ref EnvZone __instance)
            {
                if (!AutomaticDoorModPlugin.isEnabled.Value)
                {
                    return;
                }

                Player component = collider.GetComponent<Player>();
                if (component == null || Player.m_localPlayer != component)
                {
                    return;
                }

                AutomaticDoor.isInsideCrypt = false;
            }
        }

        [HarmonyPatch(typeof(Player), "Update")]
        public static class ToggleSwitch
        {
            private static bool isToggleKeyPressed()
            {
                string modKey = AutomaticDoorModPlugin.toggleSwitchModKey.Value.ToLower();
                string key = AutomaticDoorModPlugin.toggleSwitchKey.Value.ToLower();

                if(modKey.Equals("") || Input.GetKey(modKey))
                {
                    if (Input.GetKeyDown(key))
                    {
                        return true;
                    }
                }

                return false;
            }

            private static void Postfix(Player __instance)
            {
                if (!AutomaticDoorModPlugin.isEnabled.Value)
                {
                    return;
                }

                if (Player.m_localPlayer != __instance)
                {
                    return;
                }

                if (isToggleKeyPressed())
                {
                    AutomaticDoor.toggleSwitch = !AutomaticDoor.toggleSwitch;
                    if (AutomaticDoor.toggleSwitch)
                    {
                        MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, "Automatic Door Mod: Enabled");
                    }
                    else
                    {
                        MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, "Automatic Door Mod: Disabled");
                    }
                }
            }
        }
    }
}
