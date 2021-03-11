using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace AutomaticDoorMod
{
    [BepInPlugin("muro1214.valheim_mods.automatic_door", "Automatic Door Mod", toolVersion)]
    public class AutomaticDoorModPlugin : BaseUnityPlugin
    {
        public const string toolVersion = "0.1.0";
        public static bool isDebug = true;

        public static ConfigEntry<bool> isEnabled;
        public static ConfigEntry<float> waitForDoorToCloseSeconds;
        public static ConfigEntry<float> automaticDoorCloseRange;
        public static ConfigEntry<float> automaticDoorOpenRange;
        public static ConfigEntry<bool> disableAutomaticDoorOpenInCrypt;
        public static ConfigEntry<string> toggleSwitchModKey;
        public static ConfigEntry<string> toggleSwitchKey;

        public static Dictionary<int, Coroutine> coroutinePairs = new Dictionary<int, Coroutine>();

        private void Awake()
        {
            isEnabled = Config.Bind<bool>("General", "IsEnabled", true, "If you set this to false, this mod will be disabled.");
            waitForDoorToCloseSeconds = Config.Bind<float>("General", "waitForDoorToCloseSeconds", 5.0f, "Specify the time in seconds to wait for the door to close automatically.");
            automaticDoorCloseRange = Config.Bind<float>("General", "automaticDoorCloseRange", 4.0f, "Doors DO NOT CLOSE automatically when a player is in range.\nIf set to 0, the door will automatically close regardless of distance.");
            automaticDoorOpenRange = Config.Bind<float>("General", "automaticDoorOpenRange", 4.0f, "When a player is within range, the door will open automatically.\nIf set to 0, this feature is disabled.");
            disableAutomaticDoorOpenInCrypt = Config.Bind<bool>("General", "disableAutomaticDoorOpenInCrypt", true, "If set to true, disables the setting that automatically opens the door when you are inside Crypt.");
            toggleSwitchModKey = Config.Bind<string>("General", "toggleSwitchModKey", "left alt", "Specifies the MOD Key of toggleSwitchKey. If left blank, it is not used.");
            toggleSwitchKey = Config.Bind<string>("General", "toggleSwitchKey", "f10", "Toggles between enabled and disabled mods when this key is pressed.");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        [HarmonyPatch(typeof(Door), "Interact")]
        public static class AutomaticDoorClose
        {
            public static bool isInsideCrypt = false;
            public static bool toggleSwitch = true;

            private static void Postfix(ref Door __instance, ZNetView ___m_nview)
            {
                if (!isEnabled.Value || // when mod is disabled
                    __instance.m_keyItem != null || // when target door needs keyItem (e.g. CryptKey)
                    isInsideCrypt || // when player is in Crypt
                    !toggleSwitch) // when a player manually disables a mod
                {
                    return;
                }

                if(coroutinePairs.ContainsKey(___m_nview.GetHashCode()))
                {
                    ___m_nview.StopCoroutine(coroutinePairs[___m_nview.GetHashCode()]);
                }

                Debug.Log("m_doorObject pos: " + __instance.m_doorObject.transform.position);
                Debug.Log("___m_nview pos: " + ___m_nview.transform.position);
                // プレイヤーがドアの範囲内にいるときは自動で閉じない
                // 5秒経った後でも離れたタイミングで閉じる？

                Coroutine coroutine = ___m_nview.StartCoroutine(AutoCloseEnumerator(__instance.m_doorObject, ___m_nview));
                coroutinePairs[___m_nview.GetHashCode()] = coroutine;
            }

            private static IEnumerator AutoCloseEnumerator(GameObject m_doorObject, ZNetView ___m_nview)
            {
                while (true)
                {
                    yield return new WaitForSeconds(waitForDoorToCloseSeconds.Value);

                    float distance = Utils.GetPlayerDistance(m_doorObject);
                    if (distance > automaticDoorCloseRange.Value)
                    {
                        ___m_nview.GetZDO().Set("state", 0);
                        yield break;
                    }
                    else
                    {
                        Debug.Log("プレイヤーが居るから閉じないことにする");
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Door), "Awake")]
        public static class AutomaticDoorOpen
        {
            private static void Postfix(Door __instance, ref ZNetView ___m_nview)
            {
                if (!isEnabled.Value || // when mod is disabled
                    __instance.m_keyItem != null || // when target door needs keyItem (e.g. CryptKey)
                    (disableAutomaticDoorOpenInCrypt.Value && AutomaticDoorClose.isInsideCrypt) || // when player is in Crypt
                    !AutomaticDoorClose.toggleSwitch) // when a player manually disables a mod
                {
                    return;
                }

                ___m_nview.StartCoroutine(AutoOpenEnumerator(__instance, ___m_nview));
            }

            private static IEnumerator AutoOpenEnumerator(Door __instance, ZNetView ___m_nview)
            {
                bool isAlreadyEntered = false;

                while (true)
                {
                    yield return new WaitForSeconds(0.2f);

                    if (disableAutomaticDoorOpenInCrypt.Value && AutomaticDoorClose.isInsideCrypt)
                    {
                        continue;
                    }

                    Player localPlayer = Player.m_localPlayer;
                    if(localPlayer == null || __instance == null)
                    {
                        continue;
                    }

                    if (___m_nview.GetZDO().GetInt("state", 0) != 0)
                    {
                        continue;
                    }

                    float distance = Utils.GetPlayerDistance(__instance.m_doorObject);
                    if (distance <= automaticDoorOpenRange.Value && !isAlreadyEntered)
                    {
                        __instance.Interact(localPlayer, false);
                        isAlreadyEntered = true;
                    }
                    else if(distance > automaticDoorOpenRange.Value && isAlreadyEntered)
                    {
                        isAlreadyEntered = false;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(EnvZone), "OnTriggerStay")]
        public static class DisableModWhenCryptStay
        {
            private static string currentEnv = "";

            private static void Prefix(Collider collider, ref EnvZone __instance)
            {
                if (!isEnabled.Value)
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
                    AutomaticDoorClose.isInsideCrypt = true;
                    currentEnv = __instance.m_environment;
                }
            }
        }

        [HarmonyPatch(typeof(EnvZone), "OnTriggerExit")]
        public static class EnableModWhenCryptExit
        {
            private static void Prefix(Collider collider)
            {
                if (!isEnabled.Value)
                {
                    return;
                }

                Player component = collider.GetComponent<Player>();
                if (component == null || Player.m_localPlayer != component)
                {
                    return;
                }

                AutomaticDoorClose.isInsideCrypt = false;
            }
        }

        [HarmonyPatch(typeof(Player), "Update")]
        public static class ToggleSwitch
        {
            private static bool IsToggleKeyPressed()
            {
                string modKey = toggleSwitchModKey.Value.ToLower();
                string key = toggleSwitchKey.Value.ToLower();

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
                if (!isEnabled.Value)
                {
                    return;
                }

                if (Player.m_localPlayer != __instance)
                {
                    return;
                }

                if (IsToggleKeyPressed())
                {
                    AutomaticDoorClose.toggleSwitch = !AutomaticDoorClose.toggleSwitch;
                    if (AutomaticDoorClose.toggleSwitch)
                    {
                        Utils.ShowMessage("Enabled");
                    }
                    else
                    {
                        Utils.ShowMessage("Disabled");
                    }
                }
            }
        }

        public static class Utils
        {

            public static float GetPlayerDistance(GameObject m_doorObject)
            {
                return Vector3.Distance(Player.m_localPlayer.transform.position, m_doorObject.transform.position);
            }

            public static void ShowMessage(string message)
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, "Automatic Door Mod: " + message);
            }
        }

        [HarmonyPatch(typeof(ZSteamMatchmaking), "VerifySessionTicket")]
        public static class DebugModePatch1
        {
            private static bool Prefix(ref bool __result)
            {
                if (isDebug)
                {
                    __result = true;
                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(ZNet), "IsConnected")]
        public static class DebugModePatch2
        {
            private static bool Prefix(ref bool __result)
            {
                if (isDebug)
                {
                    __result = false;
                    return false;
                }

                return true;
            }
        }
    }
}
