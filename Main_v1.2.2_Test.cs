using MelonLoader;
using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Reflection.Emit;

[assembly: MelonInfo(typeof(MorePlayers.MorePlayersMod), "MorePlayers", "1.2.4", "github.com/zxzinn")]
[assembly: MelonGame("ReLUGames", "MIMESIS")]

namespace MorePlayers
{
    public class MorePlayersMod : MelonMod
    {
        public const int MAX_PLAYERS = 999;
        private static bool testRequested = false;
        private static bool testCompleted = false;
        private static float checkTimer = 0f;

        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("=================================================");
            MelonLogger.Msg("MorePlayers Mod v1.2.4 - Smart Auto Test");
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
            MelonLogger.Msg("Test will run automatically when room is ready...");
        }

        public override void OnUpdate()
        {
            if (testRequested && !testCompleted)
            {
                checkTimer += UnityEngine.Time.deltaTime;

                // Check every 2 seconds
                if (checkTimer >= 2f)
                {
                    checkTimer = 0f;
                    MelonLogger.Msg($"[DEBUG] Checking room status... (elapsed: {UnityEngine.Time.time:F1}s)");
                    TryRunTest();
                }
            }
        }

        private void TryRunTest()
        {
            try
            {
                var assembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp");

                var hubType = assembly?.GetType("Hub");
                var hubField = hubType?.GetField("s", BindingFlags.Static | BindingFlags.Public);
                var hub = hubField?.GetValue(null);

                if (hub == null) return;

                var vworldField = hubType?.GetField("vworld", BindingFlags.Instance | BindingFlags.Public);
                var vworld = vworldField?.GetValue(hub);

                if (vworld == null) return;

                var vworldType = vworld.GetType();
                var vRoomManagerProp = vworldType.GetProperty("VRoomManager",
                    BindingFlags.Instance | BindingFlags.Public);
                var roomManager = vRoomManagerProp?.GetValue(vworld);

                if (roomManager == null) return;

                var vroomManagerType = assembly?.GetType("VRoomManager");
                var vroomsField = vroomManagerType?.GetField("_vrooms",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                var vrooms = vroomsField?.GetValue(roomManager);

                if (vrooms == null) return;

                var vroomDict = vrooms as System.Collections.IDictionary;
                if (vroomDict != null && vroomDict.Count > 0)
                {
                    // Room is ready! Run test
                    testCompleted = true;
                    SteamLobbyCreation_Patch.RunTestNow();
                }
            }
            catch { }
        }

        public static void RequestTest()
        {
            if (!testRequested)
            {
                testRequested = true;
                MelonLogger.Msg("Test requested - waiting for room to initialize...");
            }
        }
    }

    // All patches from v1.2.0
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

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldc_I4_4)
                {
                    codes[i] = new CodeInstruction(OpCodes.Ldc_I4, MorePlayersMod.MAX_PLAYERS);
                    MelonLogger.Msg($"  → Changed 4 to {MorePlayersMod.MAX_PLAYERS}");
                }
            }

            return codes;
        }
    }

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

                // Request test - will run when room is ready
                MorePlayersMod.RequestTest();

                return false;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[PATCH 6] Error: {ex.Message}");
                return true;
            }
        }

        public static void RunTestNow()
        {
            try
            {
                MelonLogger.Msg("========================================");
                MelonLogger.Msg("AUTO TEST: Room is ready! Running tests...");
                MelonLogger.Msg("========================================");

                var assembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp");

                var ivroomType = assembly?.GetType("IVroom");
                var getMemberCountMethod = ivroomType?.GetMethod("GetMemberCount",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                var canEnterMethod = ivroomType?.GetMethod("CanEnterChannel",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                // Find active room
                var vroomManagerType = assembly?.GetType("VRoomManager");
                var hubType = assembly?.GetType("Hub");
                var hubField = hubType?.GetField("s", BindingFlags.Static | BindingFlags.Public);
                var hub = hubField?.GetValue(null);

                if (hub == null) return;

                var vworldField = hubType?.GetField("vworld", BindingFlags.Instance | BindingFlags.Public);
                var vworld = vworldField?.GetValue(hub);

                if (vworld == null) return;

                var vworldType = vworld.GetType();
                var vRoomManagerProp = vworldType.GetProperty("VRoomManager",
                    BindingFlags.Instance | BindingFlags.Public);
                var roomManager = vRoomManagerProp?.GetValue(vworld);

                if (roomManager == null) return;

                var vroomsField = vroomManagerType?.GetField("_vrooms",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                var vrooms = vroomsField?.GetValue(roomManager);

                if (vrooms == null) return;

                var vroomDict = vrooms as System.Collections.IDictionary;
                if (vroomDict != null && vroomDict.Count > 0)
                {
                    MelonLogger.Msg($"Found {vroomDict.Count} room(s)!");
                    foreach (var room in vroomDict.Values)
                    {
                        TestRoom(room, getMemberCountMethod, canEnterMethod);
                    }

                    MelonLogger.Msg("========================================");
                    MelonLogger.Msg("AUTO TEST: Complete!");
                    MelonLogger.Msg("========================================");
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Test failed: {ex.Message}");
                MelonLogger.Error($"Stack trace: {ex.StackTrace}");
            }
        }

        static void TestRoom(object room, MethodInfo getMemberCountMethod, MethodInfo canEnterMethod)
        {
            try
            {
                var roomType = room.GetType();
                var roomName = roomType.Name;

                // Get GetMemberCount() result
                var memberCount = getMemberCountMethod?.Invoke(room, null);

                // Get actual player dict count
                var vPlayerDictField = roomType.GetField("_vPlayerDict",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                var vPlayerDict = vPlayerDictField?.GetValue(room);
                var actualCount = 0;

                if (vPlayerDict != null)
                {
                    var countProp = vPlayerDict.GetType().GetProperty("Count");
                    actualCount = (int)countProp.GetValue(vPlayerDict);
                }

                MelonLogger.Msg($"Room: {roomName}");
                MelonLogger.Msg($"  GetMemberCount() = {memberCount} (should be 0)");
                MelonLogger.Msg($"  Actual players = {actualCount}");

                // Test CanEnterChannel
                var canEnter = roomType.GetMethod("CanEnterChannel",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                if (canEnter != null)
                {
                    MelonLogger.Msg($"  Simulating 10 players joining:");
                    int allowedCount = 0;

                    for (int i = 1; i <= 10; i++)
                    {
                        long fakeUID = 9000000 + i;
                        var result = canEnter.Invoke(room, new object[] { fakeUID });
                        var resultValue = (int)result;

                        if (resultValue == 0)
                        {
                            allowedCount++;
                            MelonLogger.Msg($"    Player {i}: ✓ ALLOWED");
                        }
                        else
                        {
                            MelonLogger.Warning($"    Player {i}: ✗ DENIED (ErrorCode: {resultValue})");
                            MelonLogger.Warning($"    ^ PATCH FAILED! Players blocked at #{i}");
                            break;
                        }
                    }

                    if (allowedCount >= 10)
                    {
                        MelonLogger.Msg("");
                        MelonLogger.Msg("  ✓✓✓ SUCCESS! All 10 players allowed!");
                        MelonLogger.Msg("  ✓✓✓ Patch is working correctly!");
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Room test error: {ex.Message}");
            }
        }
    }
}
