using MelonLoader;
using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Reflection.Emit;

[assembly: MelonInfo(typeof(MorePlayers.MorePlayersMod), "MorePlayers", "1.1.0", "github.com/zxzinn")]
[assembly: MelonGame("ReLUGames", "MIMESIS")]

namespace MorePlayers
{
    public class MorePlayersMod : MelonMod
    {
        public const int MAX_PLAYERS = 999;

        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("=================================================");
            MelonLogger.Msg("MorePlayers Mod v1.1.0 - Complete Fix");
            MelonLogger.Msg("=================================================");
            MelonLogger.Msg("Author: github.com/zxzinn");
            MelonLogger.Msg($"Max Players: {MAX_PLAYERS}");
            MelonLogger.Msg("");
            MelonLogger.Msg("Patching ALL 4-player limit checks...");

            var harmony = new HarmonyLib.Harmony("com.moreplayers.mod");
            harmony.PatchAll(typeof(MorePlayersMod).Assembly);

            MelonLogger.Msg("=================================================");
            MelonLogger.Msg("All patches applied successfully!");
            MelonLogger.Msg("=================================================");
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
                    MelonLogger.Msg("[PATCH 1] ServerSocket.GetMaximumClients: FOUND");

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
                    MelonLogger.Msg("[PATCH 2] ServerSocket.SetMaximumClients: FOUND");

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
                    MelonLogger.Msg("[PATCH 3] ServerSocket Constructor: FOUND");

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

    // PATCH 4: IVroom.CanEnterChannel - Remove >= 4 check
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
                    MelonLogger.Msg("[PATCH 4] IVroom.CanEnterChannel: FOUND");

                return method;
            }
            catch { return null; }
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            for (int i = 0; i < codes.Count - 2; i++)
            {
                // Find: if (_vPlayerDict.Count >= 4)
                // Pattern: ldc.i4.4 (load 4), followed by comparison
                if (codes[i].opcode == OpCodes.Ldc_I4_4)
                {
                    // Change 4 to MAX_PLAYERS
                    codes[i] = new CodeInstruction(OpCodes.Ldc_I4, MorePlayersMod.MAX_PLAYERS);
                    MelonLogger.Msg($"[PATCH 4] Transpiled: Changed hardcoded 4 to {MorePlayersMod.MAX_PLAYERS}");
                }
            }

            return codes;
        }
    }

    // PATCH 5: VRoomManager.EnterMaintenanceRoom - Remove >= 4 check
    [HarmonyPatch]
    public class VRoomManager_EnterMaintenanceRoom_Patch
    {
        static MethodBase TargetMethod()
        {
            try
            {
                var assembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp");
                var vroomManagerType = assembly?.GetType("VRoomManager");
                var method = vroomManagerType?.GetMethod("EnterMaintenanceRoom",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                if (method != null)
                    MelonLogger.Msg("[PATCH 5] VRoomManager.EnterMaintenanceRoom: FOUND");

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
                    MelonLogger.Msg($"[PATCH 5] Transpiled: Changed hardcoded 4 to {MorePlayersMod.MAX_PLAYERS}");
                }
            }

            return codes;
        }
    }

    // PATCH 6: VRoomManager.EnterWaitingRoom - Remove >= 4 check
    [HarmonyPatch]
    public class VRoomManager_EnterWaitingRoom_Patch
    {
        static MethodBase TargetMethod()
        {
            try
            {
                var assembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp");
                var vroomManagerType = assembly?.GetType("VRoomManager");
                var method = vroomManagerType?.GetMethod("EnterWaitingRoom",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                if (method != null)
                    MelonLogger.Msg("[PATCH 6] VRoomManager.EnterWaitingRoom: FOUND");

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
                    MelonLogger.Msg($"[PATCH 6] Transpiled: Changed hardcoded 4 to {MorePlayersMod.MAX_PLAYERS}");
                }
            }

            return codes;
        }
    }

    // PATCH 7: VRoomManager.InitWaitingRoom - Remove == 4 check
    [HarmonyPatch]
    public class VRoomManager_InitWaitingRoom_Patch
    {
        static MethodBase TargetMethod()
        {
            try
            {
                var assembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp");
                var vroomManagerType = assembly?.GetType("VRoomManager");
                var method = vroomManagerType?.GetMethod("InitWaitingRoom",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                if (method != null)
                    MelonLogger.Msg("[PATCH 7] VRoomManager.InitWaitingRoom: FOUND");

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
                    MelonLogger.Msg($"[PATCH 7] Transpiled: Changed hardcoded 4 to {MorePlayersMod.MAX_PLAYERS}");
                }
            }

            return codes;
        }
    }

    // PATCH 8: CreateLobby - Set Steam lobby max
    [HarmonyPatch(typeof(SteamInviteDispatcher), "CreateLobby")]
    public class SteamLobbyCreation_Patch
    {
        static bool Prefix(bool isOpenForRandomMatch)
        {
            try
            {
                MelonLogger.Msg("[PATCH 8] CreateLobby intercepted");

                var steamMatchmakingType = Type.GetType("Steamworks.SteamMatchmaking, com.rlabrecque.steamworks.net");
                var eLobbyTypeType = Type.GetType("Steamworks.ELobbyType, com.rlabrecque.steamworks.net");
                var playerPrefsType = Type.GetType("UnityEngine.PlayerPrefs, UnityEngine.CoreModule");

                if (steamMatchmakingType == null || eLobbyTypeType == null || playerPrefsType == null)
                {
                    MelonLogger.Error("[PATCH 8] Failed to get required types");
                    return true;
                }

                var createLobbyMethod = steamMatchmakingType.GetMethod("CreateLobby",
                    BindingFlags.Public | BindingFlags.Static);
                var setIntMethod = playerPrefsType.GetMethod("SetInt",
                    BindingFlags.Public | BindingFlags.Static);

                if (createLobbyMethod == null || setIntMethod == null)
                {
                    MelonLogger.Error("[PATCH 8] Failed to get required methods");
                    return true;
                }

                // ELobbyType.FriendsOnly = 2
                var friendsOnly = Enum.ToObject(eLobbyTypeType, 2);
                createLobbyMethod.Invoke(null, new object[] { friendsOnly, MorePlayersMod.MAX_PLAYERS });
                setIntMethod.Invoke(null, new object[] { "TempLobbyIsOpen", isOpenForRandomMatch ? 1 : 0 });

                MelonLogger.Msg($"[PATCH 8] Steam lobby created with {MorePlayersMod.MAX_PLAYERS} slots!");

                return false;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[PATCH 8] Failed: {ex.Message}");
                return true;
            }
        }
    }
}
