using MelonLoader;
using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Reflection.Emit;

[assembly: MelonInfo(typeof(MorePlayers.MorePlayersMod), "MorePlayers", "1.3.1", "github.com/zxzinn")]
[assembly: MelonGame("ReLUGames", "MIMESIS")]

namespace MorePlayers
{
    public class MorePlayersMod : MelonMod
    {
        public const int MAX_PLAYERS = 999;

        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("=================================================");
            MelonLogger.Msg("MorePlayers Mod v1.3.1 - Aggressive Debug (Fixed)");
            MelonLogger.Msg("=================================================");
            MelonLogger.Msg("Author: github.com/zxzinn");
            MelonLogger.Msg($"Max Players: {MAX_PLAYERS}");
            MelonLogger.Msg("");
            MelonLogger.Msg("Patching all player limit checks...");

            var harmony = new HarmonyLib.Harmony("com.moreplayers.mod");
            harmony.PatchAll(typeof(MorePlayersMod).Assembly);

            MelonLogger.Msg("=================================================");
            MelonLogger.Msg("All patches applied!");
            MelonLogger.Msg("=================================================");
            MelonLogger.Msg("");
            MelonLogger.Msg("Will log when players try to join...");
        }
    }

    // PATCH 1: ServerSocket.GetMaximumClients
    [HarmonyPatch]
    public class GetMaximumClients_Patch
    {
        static MethodBase TargetMethod()
        {
            try
            {
                var assembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp");
                var serverSocketType = assembly?.GetType("FishySteamworks.Server.ServerSocket");
                var method = serverSocketType?.GetMethod("GetMaximumClients",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                if (method != null)
                    MelonLogger.Msg("[✓ PATCH 1] ServerSocket.GetMaximumClients");

                return method;
            }
            catch { return null; }
        }

        static bool Prefix(ref int __result)
        {
            __result = MorePlayersMod.MAX_PLAYERS;
            return false;
        }
    }

    // PATCH 2: ServerSocket.SetMaximumClients
    [HarmonyPatch]
    public class SetMaximumClients_Patch
    {
        static MethodBase TargetMethod()
        {
            try
            {
                var assembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp");
                var serverSocketType = assembly?.GetType("FishySteamworks.Server.ServerSocket");
                var method = serverSocketType?.GetMethod("SetMaximumClients",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                if (method != null)
                    MelonLogger.Msg("[✓ PATCH 2] ServerSocket.SetMaximumClients");

                return method;
            }
            catch { return null; }
        }

        static bool Prefix(ref int value)
        {
            if (value < MorePlayersMod.MAX_PLAYERS)
            {
                value = MorePlayersMod.MAX_PLAYERS;
            }
            return true;
        }
    }

    // PATCH 3: ServerSocket Constructor
    [HarmonyPatch]
    public class ServerSocket_Constructor_Patch
    {
        static MethodBase TargetMethod()
        {
            try
            {
                var assembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp");
                var serverSocketType = assembly?.GetType("FishySteamworks.Server.ServerSocket");
                var ctor = serverSocketType?.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .FirstOrDefault();

                if (ctor != null)
                    MelonLogger.Msg("[✓ PATCH 3] ServerSocket Constructor");

                return ctor;
            }
            catch { return null; }
        }

        static void Postfix(object __instance)
        {
            try
            {
                var type = __instance.GetType();
                var setMethod = type.GetMethod("SetMaximumClients",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                setMethod?.Invoke(__instance, new object[] { MorePlayersMod.MAX_PLAYERS });
            }
            catch { }
        }
    }

    // PATCH 4: IVroom.CanEnterChannel - 激進的 Prefix + Transpiler 雙重攻擊
    [HarmonyPatch]
    public class IVroom_CanEnterChannel_Patch
    {
        static MethodBase TargetMethod()
        {
            try
            {
                var assembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp");
                var ivroomType = assembly?.GetType("IVroom");
                var method = ivroomType?.GetMethod("CanEnterChannel",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                if (method != null)
                    MelonLogger.Msg("[✓ PATCH 4] IVroom.CanEnterChannel");

                return method;
            }
            catch { return null; }
        }

        // 在方法執行前攔截並記錄
        static void Prefix(object __instance, long playerUID)
        {
            try
            {
                var roomType = __instance.GetType();
                var vPlayerDictField = roomType.GetField("_vPlayerDict",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                var vPlayerDict = vPlayerDictField?.GetValue(__instance);

                if (vPlayerDict != null)
                {
                    var countProp = vPlayerDict.GetType().GetProperty("Count");
                    var actualCount = (int)countProp.GetValue(vPlayerDict);

                    MelonLogger.Msg("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                    MelonLogger.Msg($"[PLAYER JOIN] Room: {roomType.Name}");
                    MelonLogger.Msg($"[PLAYER JOIN] PlayerUID: {playerUID}");
                    MelonLogger.Msg($"[PLAYER JOIN] Current players in room: {actualCount}");
                    MelonLogger.Msg("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                }
            }
            catch { }
        }

        // 在方法執行後攔截結果
        static void Postfix(object __instance, long playerUID, ref object __result)
        {
            try
            {
                var roomType = __instance.GetType();

                // 轉換 enum 為 int
                int errorCode = Convert.ToInt32(__result);

                if (errorCode == 0)
                {
                    MelonLogger.Msg($"[PLAYER JOIN] ✓✓✓ Player {playerUID} ALLOWED into {roomType.Name}");
                }
                else
                {
                    MelonLogger.Error($"[PLAYER JOIN] ✗✗✗ Player {playerUID} DENIED! ErrorCode: {errorCode} ({__result})");
                    MelonLogger.Error($"[PLAYER JOIN] ✗✗✗ Room: {roomType.Name}");

                    // 強制允許進入！找到 MsgErrorCode.Success
                    var assembly = AppDomain.CurrentDomain.GetAssemblies()
                        .FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp");
                    var msgErrorCodeType = assembly?.GetType("ReluProtocol.Enum.MsgErrorCode");

                    if (msgErrorCodeType != null)
                    {
                        var successValue = Enum.ToObject(msgErrorCodeType, 0);
                        MelonLogger.Warning($"[PLAYER JOIN] >>> FORCING ALLOW (changing {errorCode} to MsgErrorCode.Success)");
                        __result = successValue;
                    }
                }

                MelonLogger.Msg("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[PLAYER JOIN] Postfix error: {ex.Message}");
            }
        }

        // IL Transpiler 修改硬編碼的 4
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldc_I4_4)
                {
                    codes[i] = new CodeInstruction(OpCodes.Ldc_I4, MorePlayersMod.MAX_PLAYERS);
                    MelonLogger.Msg($"  → IL Transpiler: Changed 4 to {MorePlayersMod.MAX_PLAYERS}");
                }
            }

            return codes;
        }
    }

    // PATCH 5: IVroom.GetMemberCount - 返回 0
    [HarmonyPatch]
    public class IVroom_GetMemberCount_Patch
    {
        static MethodBase TargetMethod()
        {
            try
            {
                var assembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp");
                var ivroomType = assembly?.GetType("IVroom");
                var method = ivroomType?.GetMethod("GetMemberCount",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                if (method != null)
                    MelonLogger.Msg("[✓ PATCH 5] IVroom.GetMemberCount");

                return method;
            }
            catch { return null; }
        }

        static bool Prefix(ref int __result)
        {
            __result = 0;
            return false;
        }
    }

    // PATCH 6: CreateLobby
    [HarmonyPatch(typeof(SteamInviteDispatcher), "CreateLobby")]
    public class SteamLobbyCreation_Patch
    {
        static bool Prefix(bool isOpenForRandomMatch)
        {
            try
            {
                var steamMatchmakingType = Type.GetType("Steamworks.SteamMatchmaking, com.rlabrecque.steamworks.net");
                var eLobbyTypeType = Type.GetType("Steamworks.ELobbyType, com.rlabrecque.steamworks.net");
                var playerPrefsType = Type.GetType("UnityEngine.PlayerPrefs, UnityEngine.CoreModule");

                if (steamMatchmakingType == null || eLobbyTypeType == null || playerPrefsType == null)
                {
                    return true;
                }

                var createLobbyMethod = steamMatchmakingType.GetMethod("CreateLobby",
                    BindingFlags.Public | BindingFlags.Static);
                var setIntMethod = playerPrefsType.GetMethod("SetInt",
                    BindingFlags.Public | BindingFlags.Static);

                if (createLobbyMethod == null || setIntMethod == null)
                {
                    return true;
                }

                MelonLogger.Msg($"[✓ PATCH 6] Steam lobby: {MorePlayersMod.MAX_PLAYERS} slots");

                var friendsOnly = Enum.ToObject(eLobbyTypeType, 2);
                createLobbyMethod.Invoke(null, new object[] { friendsOnly, MorePlayersMod.MAX_PLAYERS });
                setIntMethod.Invoke(null, new object[] { "TempLobbyIsOpen", isOpenForRandomMatch ? 1 : 0 });

                return false;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[PATCH 6] Error: {ex.Message}");
                return true;
            }
        }
    }

    // PATCH 7: 攔截所有 ProcessEnterWaitQueue 調用
    [HarmonyPatch]
    public class ProcessEnterWaitQueue_Patch
    {
        static MethodBase TargetMethod()
        {
            try
            {
                var assembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp");
                var ivroomType = assembly?.GetType("IVroom");
                var method = ivroomType?.GetMethod("ProcessEnterWaitQueue",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                if (method != null)
                    MelonLogger.Msg("[✓ PATCH 7] IVroom.ProcessEnterWaitQueue");

                return method;
            }
            catch { return null; }
        }

        static void Prefix(object __instance)
        {
            try
            {
                var roomType = __instance.GetType();
                MelonLogger.Msg($"[PROCESS QUEUE] Processing enter queue for {roomType.Name}");
            }
            catch { }
        }
    }
}
