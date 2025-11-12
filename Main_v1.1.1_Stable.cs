using MelonLoader;
using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Reflection.Emit;

[assembly: MelonInfo(typeof(MorePlayers.MorePlayersMod), "MorePlayers", "1.1.1", "github.com/zxzinn")]
[assembly: MelonGame("ReLUGames", "MIMESIS")]

namespace MorePlayers
{
    public class MorePlayersMod : MelonMod
    {
        public const int MAX_PLAYERS = 999;

        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("=================================================");
            MelonLogger.Msg("MorePlayers Mod v1.1.1 - Stable Fix");
            MelonLogger.Msg("=================================================");
            MelonLogger.Msg("Author: github.com/zxzinn");
            MelonLogger.Msg($"Max Players: {MAX_PLAYERS}");
            MelonLogger.Msg("");
            MelonLogger.Msg("Applying verified patches only...");

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

    // PATCH 4: IVroom.CanEnterChannel - THE CRITICAL PATCH
    // This is the main 4-player limit check that prevents 5th player from joining
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
                    MelonLogger.Msg("[✓ PATCH 4] IVroom.CanEnterChannel - CRITICAL!");

                return method;
            }
            catch { return null; }
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            int patchCount = 0;

            for (int i = 0; i < codes.Count; i++)
            {
                // Find: ldc.i4.4 (load constant 4)
                if (codes[i].opcode == OpCodes.Ldc_I4_4)
                {
                    // Replace with: ldc.i4 999
                    codes[i] = new CodeInstruction(OpCodes.Ldc_I4, MorePlayersMod.MAX_PLAYERS);
                    patchCount++;
                    MelonLogger.Msg($"  → Replaced hardcoded 4 with {MorePlayersMod.MAX_PLAYERS}");
                }
            }

            if (patchCount == 0)
            {
                MelonLogger.Warning("  ⚠ No hardcoded 4 found to patch!");
            }

            return codes;
        }
    }

    // PATCH 5: CreateLobby - Set Steam lobby max
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
                    MelonLogger.Error("[✗ PATCH 5] Failed to get required types");
                    return true;
                }

                var createLobbyMethod = steamMatchmakingType.GetMethod("CreateLobby",
                    BindingFlags.Public | BindingFlags.Static);
                var setIntMethod = playerPrefsType.GetMethod("SetInt",
                    BindingFlags.Public | BindingFlags.Static);

                if (createLobbyMethod == null || setIntMethod == null)
                {
                    MelonLogger.Error("[✗ PATCH 5] Failed to get required methods");
                    return true;
                }

                MelonLogger.Msg($"[✓ PATCH 5] Steam lobby: {MorePlayersMod.MAX_PLAYERS} slots");

                // ELobbyType.FriendsOnly = 2
                var friendsOnly = Enum.ToObject(eLobbyTypeType, 2);
                createLobbyMethod.Invoke(null, new object[] { friendsOnly, MorePlayersMod.MAX_PLAYERS });
                setIntMethod.Invoke(null, new object[] { "TempLobbyIsOpen", isOpenForRandomMatch ? 1 : 0 });

                return false;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[✗ PATCH 5] Exception: {ex.Message}");
                return true;
            }
        }
    }
}
