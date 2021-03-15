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
        public const string toolVersion = "0.1.1.8";
        // デバッグ用フラグ。リリース時はfalseにする
        private static bool isDebug = true;

        // MODが有効化されているか？
        public static ConfigEntry<bool> isEnabled;

        // ドアを開いてから自動で閉じるまでの待ち時間
        public static ConfigEntry<float> waitForDoorToCloseSeconds;

        // この範囲内にプレイヤーが居るときはドアを閉じない(廃止)
        public static ConfigEntry<float> automaticDoorCloseRange;
        // 以下のドア種別ごとのレンジに変更
        public static ConfigEntry<float> automaticDoorCloseRange_Door;
        public static ConfigEntry<float> automaticDoorCloseRange_Gate;
        public static ConfigEntry<float> automaticDoorCloseRange_IronGate;

        // この範囲内にプレイヤーが居るときにドアを自動で開く(廃止)
        public static ConfigEntry<float> automaticDoorOpenRange;
        // 以下のドア種別ごとのレンジに変更
        public static ConfigEntry<float> automaticDoorOpenRange_Door;
        public static ConfigEntry<float> automaticDoorOpenRange_Gate;
        public static ConfigEntry<float> automaticDoorOpenRange_IronGate;

        // Crypt内にいるときに自動でドアを開くか？
        public static ConfigEntry<bool> disableAutomaticDoorOpenInCrypt;

        // ホットキー
        public static ConfigEntry<string> toggleSwitchModKey;
        public static ConfigEntry<string> toggleSwitchKey;

        // ドアと実行中のコルーチンの組み合わせ
        private static Dictionary<int, Coroutine> coroutinePairs = new Dictionary<int, Coroutine>();

        private static bool isInsideCrypt = false;
        private static bool toggleSwitch = true;

        private static AutomaticDoorModPlugin instance;

        // プラグインの初期設定
        private void Awake()
        {
            instance = this;

            isEnabled = Config.Bind<bool>("General", "IsEnabled", true, "If you set this to false, this mod will be disabled.");

            waitForDoorToCloseSeconds = Config.Bind<float>("General", "waitForDoorToCloseSeconds", 5.0f, "Specify the time in seconds to wait for the door to close automatically.");
            automaticDoorCloseRange = Config.Bind<float>("General", "automaticDoorCloseRange", 0.0f, "Obsolete");
            automaticDoorCloseRange_Door = Config.Bind<float>("General", "automaticDoorCloseRange_Door", 3.0f, "DOOR DO NOT CLOSE automatically when a player is in range.\nIf set to 0, the door will automatically close regardless of distance.");
            automaticDoorCloseRange_Gate = Config.Bind<float>("General", "automaticDoorCloseRange_Gate", 4.0f, "GATE DO NOT CLOSE automatically when a player is in range.\nIf set to 0, the door will automatically close regardless of distance.");
            automaticDoorCloseRange_IronGate = Config.Bind<float>("General", "automaticDoorCloseRange_IronGate", 4.0f, "IRON GATE DO NOT CLOSE automatically when a player is in range.\nIf set to 0, the door will automatically close regardless of distance.");

            automaticDoorOpenRange = Config.Bind<float>("General", "automaticDoorOpenRange", 0.0f, "Obsolete");
            automaticDoorOpenRange_Door = Config.Bind<float>("General", "automaticDoorOpenRange_Door", 3.0f, "When a player is within range, the DOOR will open automatically.\nIf set to 0, this feature is disabled.");
            automaticDoorOpenRange_Gate = Config.Bind<float>("General", "automaticDoorOpenRange_Gate", 4.0f, "When a player is within range, the GATE will open automatically.\nIf set to 0, this feature is disabled.");
            automaticDoorOpenRange_IronGate = Config.Bind<float>("General", "automaticDoorOpenRange_IronGate", 4.0f, "When a player is within range, the IRON GATE will open automatically.\nIf set to 0, this feature is disabled.");

            disableAutomaticDoorOpenInCrypt = Config.Bind<bool>("General", "disableAutomaticDoorOpenInCrypt", true, "If set to true, disables the setting that automatically opens the door when you are inside Crypt.");

            toggleSwitchModKey = Config.Bind<string>("General", "toggleSwitchModKey", "left alt", "Specifies the MOD Key of toggleSwitchKey. If left blank, it is not used.\nIf both toggleSwitchModKey and toggleSwitchKey are left blank, the hotkey function will be disabled.");
            toggleSwitchKey = Config.Bind<string>("General", "toggleSwitchKey", "f10", "Toggles between enabled and disabled mods when this key is pressed.\nIf both toggleSwitchModKey and toggleSwitchKey are left blank, the hotkey function will be disabled.");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        // 自動でドアを閉じる処理
        [HarmonyPatch(typeof(Door), "Interact")]
        public static class AutomaticDoorClose
        {
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

                Coroutine coroutine = ___m_nview.StartCoroutine(AutoCloseEnumerator(__instance, ___m_nview));
                coroutinePairs[___m_nview.GetHashCode()] = coroutine;
            }

            public static IEnumerator AutoCloseEnumerator(Door __instance, ZNetView ___m_nview)
            {
                while (true)
                {
                    // 一定時間待機
                    yield return new WaitForSeconds(waitForDoorToCloseSeconds.Value);

                    if (!toggleSwitch) // トグルスイッチでMODが無効化されている
                    {
                        coroutinePairs.Remove(___m_nview.GetHashCode());
                        yield break;
                    }

                    // ドア種別ごとの設定値
                    String doorName = __instance.m_name;
                    float distanceSetting = Utils.GetSettingRangeByDoor(doorName, false);

                    // プレイヤーとの距離を取得し、指定された範囲より離れているときはドアを閉じる
                    float distance = Utils.GetPlayerDistance(__instance.m_doorObject);
                    if (distance > distanceSetting)
                    {
                        ___m_nview.GetZDO().Set("state", 0);
                        coroutinePairs.Remove(___m_nview.GetHashCode());
                        yield break;
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
                if (!isEnabled.Value) // MODが無効化されている
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

                    if (__instance.m_keyItem != null || // 対象のドアに鍵が必要
                        (disableAutomaticDoorOpenInCrypt.Value && isInsideCrypt) || // プレイヤーがCrypt内にいる
                        !toggleSwitch) // トグルスイッチでMODが無効化されている
                    {
                        continue;
                    }

                    // ログイン中はインスタンスが取得できないので何もしない
                    Player localPlayer = Player.m_localPlayer;
                    if (localPlayer == null || __instance == null || ___m_nview == null)
                    {
                        continue;
                    }

                    // すでにドアが開いているときは一定時間後に閉じる処理を起動
                    // TODO; Crypt内でも閉じちゃうかも
                    if (___m_nview.GetZDO().GetInt("state", 0) != 0)
                    {
                        if (!coroutinePairs.ContainsKey(___m_nview.GetHashCode()) && !isInsideCrypt)
                        {
                            DebugLog("コルーチン起動");
                            Coroutine coroutine = ___m_nview.StartCoroutine(AutomaticDoorClose.AutoCloseEnumerator(__instance, ___m_nview));
                            coroutinePairs[___m_nview.GetHashCode()] = coroutine;
                        }
                        continue;
                    }

                    // ドア種別ごとのdistanceサポート
                    String doorName = __instance.m_name;
                    float distanceSetting = Utils.GetSettingRangeByDoor(doorName, true);

                    // プレイヤーがドアの範囲内にいる、かつ、初めてプレイヤーが近づいたときにドアを開く
                    float distance = Utils.GetPlayerDistance(__instance.m_doorObject);
                    if (distance <= distanceSetting && !isAlreadyEntered)
                    {
                        DebugLog("___m_nview.name: " + ___m_nview.name);
                        __instance.Interact(localPlayer, false);
                        isAlreadyEntered = true;
                    }
                    else if (distance > distanceSetting && isAlreadyEntered)
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
                isInsideCrypt = ___m_forceEnv.Contains("Crypt");
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

                // 両方空白の場合は何もしない
                if(modKey.Equals("") && key.Equals(""))
                {
                    return false;
                }

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
                    toggleSwitch = !toggleSwitch;
                    if (toggleSwitch)
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

            // ドア種別ごとの設定値を返す
            public static float GetSettingRangeByDoor(string doorName, bool isOpen)
            {
                // [Info: Unity Log] __instance.m_name: Door
                // [Info: Unity Log] __instance.m_name: Gate
                // [Info: Unity Log] __instance.m_name: $piece_irongate

                float distanceSetting = 0.0f;
                if (doorName.Equals("Door"))
                {
                    distanceSetting = isOpen ? automaticDoorOpenRange_Door.Value : automaticDoorCloseRange_Door.Value;
                }
                else if (doorName.Equals("Gate"))
                {
                    distanceSetting = isOpen ? automaticDoorOpenRange_Gate.Value : automaticDoorCloseRange_Gate.Value;
                }
                else if (doorName.Equals("$piece_irongate"))
                {
                    distanceSetting = isOpen ? automaticDoorOpenRange_IronGate.Value : automaticDoorCloseRange_IronGate.Value;
                }

                return distanceSetting;
            }
        }

        /** ---- 以下デバッグ用に使用するやつ -- **/

        // デバッグ中のログ表示
        public static void DebugLog(string message)
        {
            if (isDebug)
            {
                Debug.Log(message);
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

        [HarmonyPatch(typeof(Console), "InputText")]
        public static class ConfigReloader
        {
            private static bool Prefix(ref Console __instance)
            {
                if(!isEnabled.Value || !isDebug)
                {
                    return true;
                }

                string text = __instance.m_input.text;
                if(text.ToLower().Equals("/automatic_door_mod reload"))
                {
                    instance.Config.Reload();
                    instance.Config.Save();
                    Traverse.Create(__instance).Method("AddString", new object[] { "Reload muro1214.valheim_mods.automatic_door.cfg" }).GetValue();
                    return false;
                }

                return true;
            }
        }
    }
}
