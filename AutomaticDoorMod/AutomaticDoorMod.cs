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

        private void Awake()
        {
            isEnabled = Config.Bind<bool>("General", "IsEnabled", true, "If you set this to false, this mod will be disabled.");
            waitForDoorToCloseSeconds = Config.Bind<float>("General", "waitForDoorToCloseSeconds", 5, "Specify the time in seconds to wait for the door to close automatically.");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        [HarmonyPatch(typeof(Door), "Interact")]
        public static class AutomaticDoor
        {
            public static bool isInsideCrypt = false;

            private static void Postfix(ref Door __instance, ZNetView ___m_nview, ref ItemDrop ___m_keyItem)
            {
                // ___m_keyItem: CryptKey
                if (!AutomaticDoorModPlugin.isEnabled.Value || ___m_keyItem != null || isInsideCrypt)
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

        [HarmonyPatch(typeof(EnvMan), "SetForceEnvironment")]
        public static class SwitchModEnable
        {
            private static void Prefix(string env, ref string ___m_forceEnv)
            {
                if (!AutomaticDoorModPlugin.isEnabled.Value)
                {
                    return;
                }

                if(___m_forceEnv == env)
                {
                    return;
                }

                AutomaticDoor.isInsideCrypt = env.Contains("Crypt");
                if (AutomaticDoor.isInsideCrypt)
                {
                    Debug.Log("MOD disabled because player has entered Crypt.");
                }
                else
                {
                    Debug.Log("MOD enabled because player has exited Crypt.");
                }
            }
        }
    }
}
