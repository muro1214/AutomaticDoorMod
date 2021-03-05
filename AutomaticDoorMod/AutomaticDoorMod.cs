using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace AutomaticDoorMod
{
    [BepInPlugin("muro1214.valheim_mods.automatic_door", "Automatic Door Mod", "0.0.1")]
    public class AutomaticDoorMod : BaseUnityPlugin
    {
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
            private static void Postfix(ZNetView ___m_nview, ref ItemDrop ___m_keyItem)
            {
                if (!AutomaticDoorMod.isEnabled.Value || ___m_keyItem != null)
                {
                    return;
                }

                IEnumerator enumerator = DoorCloseDelay(AutomaticDoorMod.waitForDoorToCloseSeconds.Value, () =>
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
    }
}
