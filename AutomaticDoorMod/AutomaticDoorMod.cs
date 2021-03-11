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
        // デバッグ用フラグ。リリース時はfalseにする
        public static bool isDebug = true;

        // MODが有効化されているか？
        public static ConfigEntry<bool> isEnabled;
        // ドアを開いてから自動で閉じるまでの待ち時間
        public static ConfigEntry<float> waitForDoorToCloseSeconds;
        // この範囲内にプレイヤーが居るときはドアを閉じない
        public static ConfigEntry<float> automaticDoorCloseRange;
        // この範囲内にプレイヤーが居るときにドアを自動で開く
        public static ConfigEntry<float> automaticDoorOpenRange;
        // Crypt内にいるときに自動でドアを開くか？
        public static ConfigEntry<bool> disableAutomaticDoorOpenInCrypt;
        // ホットキー
        public static ConfigEntry<string> toggleSwitchModKey;
        public static ConfigEntry<string> toggleSwitchKey;

        // ドアと実行中のコルーチンの組み合わせ
        public static Dictionary<int, Coroutine> coroutinePairs = new Dictionary<int, Coroutine>();

        // デバッグ中のログ表示
        public static void DebugLog(string message)
        {
            if (isDebug)
            {
                Debug.Log(message);
            }
        }

        // プラグインの初期設定
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

        // 自動でドアを閉じる処理
        [HarmonyPatch(typeof(Door), "Interact")]
        public static class AutomaticDoorClose
        {
            public static bool isInsideCrypt = false;
            public static bool toggleSwitch = true;

            private static void Postfix(ref Door __instance, ZNetView ___m_nview)
            {
                if (!isEnabled.Value || // MODが無効化されている
                    __instance.m_keyItem != null || // 対象のドアに鍵が必要
                    isInsideCrypt || // プレイヤーがCrypt内にいる
                    !toggleSwitch) // トグルスイッチでMODが無効化されている
                {
                    return;
                }

                if(coroutinePairs.ContainsKey(___m_nview.GetHashCode()))
                {
                    ___m_nview.StopCoroutine(coroutinePairs[___m_nview.GetHashCode()]);
                }

                DebugLog("m_doorObject pos: " + __instance.m_doorObject.transform.position);
                DebugLog("___m_nview pos: " + ___m_nview.transform.position);
                // TODO;
                // プレイヤーがドアの範囲内にいるときは自動で閉じない
                // 5秒経った後でも離れたタイミングで閉じる？

                Coroutine coroutine = ___m_nview.StartCoroutine(AutoCloseEnumerator(__instance.m_doorObject, ___m_nview));
                coroutinePairs[___m_nview.GetHashCode()] = coroutine;
            }

            private static IEnumerator AutoCloseEnumerator(GameObject m_doorObject, ZNetView ___m_nview)
            {
                while (true)
                {
                    // 一定時間待機
                    yield return new WaitForSeconds(waitForDoorToCloseSeconds.Value);

                    // プレイヤーとの距離を取得し、指定された範囲より離れているときはドアを閉じる
                    float distance = Utils.GetPlayerDistance(m_doorObject);
                    if (distance > automaticDoorCloseRange.Value)
                    {
                        ___m_nview.GetZDO().Set("state", 0);
                        yield break;
                    }
                    else
                    {
                        DebugLog("プレイヤーが居るから閉じないことにする");
                    }
                }
            }
        }

        // 自動でドアを開く処理
        [HarmonyPatch(typeof(Door), "Awake")]
        public static class AutomaticDoorOpen
        {
            private static void Postfix(Door __instance, ref ZNetView ___m_nview)
            {
                if (!isEnabled.Value || // MODが無効化されている
                    __instance.m_keyItem != null || // 対象のドアに鍵が必要
                    (disableAutomaticDoorOpenInCrypt.Value && AutomaticDoorClose.isInsideCrypt) || // プレイヤーがCrypt内にいる
                    !AutomaticDoorClose.toggleSwitch) // トグルスイッチでMODが無効化されている
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
                    // 一定時間待機
                    yield return new WaitForSeconds(0.2f);

                    // プレイヤーがCrypt内にいる
                    if (disableAutomaticDoorOpenInCrypt.Value && AutomaticDoorClose.isInsideCrypt)
                    {
                        continue;
                    }

                    // ログイン中はインスタンスが取得できないので何もしない
                    Player localPlayer = Player.m_localPlayer;
                    if (localPlayer == null || __instance == null)
                    {
                        continue;
                    }

                    // すでにドアが開いているときは何もしない
                    if (___m_nview.GetZDO().GetInt("state", 0) != 0)
                    {
                        continue;
                    }

                    // プレイヤーがドアの範囲内にいる、かつ、初めてプレイヤーが近づいたときにドアを開く
                    float distance = Utils.GetPlayerDistance(__instance.m_doorObject);
                    if (distance <= automaticDoorOpenRange.Value && !isAlreadyEntered)
                    {
                        __instance.Interact(localPlayer, false);
                        isAlreadyEntered = true;
                    }
                    else if (distance > automaticDoorOpenRange.Value && isAlreadyEntered)
                    {
                        isAlreadyEntered = false;
                    }
                }
            }
        }

        // プレイヤーがCrypt内にいるか？
        [HarmonyPatch(typeof(EnvMan), "SetForceEnvironment")]
        public static class SetForceEnvironmentPatch
        {
            private static void Postfix(string ___m_forceEnv)
            {
                AutomaticDoorClose.isInsideCrypt = ___m_forceEnv.Contains("Crypt");
            }
        }

        // ホットキーの処理
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

        // ゆーてりてー
        public static class Utils
        {

            // 対象のオブジェクトとプレイヤーの距離を返す
            public static float GetPlayerDistance(GameObject m_doorObject)
            {
                return Vector3.Distance(Player.m_localPlayer.transform.position, m_doorObject.transform.position);
            }

            // 画面左上にメッセージを出す
            public static void ShowMessage(string message)
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, "Automatic Door Mod: " + message);
            }
        }

        // 同一クライアントで複数ログインできるようにするやつ1
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

        // 同一クライアントで複数ログインできるようにするやつ2
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
