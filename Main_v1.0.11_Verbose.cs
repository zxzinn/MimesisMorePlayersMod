using MelonLoader;
using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;

[assembly: MelonInfo(typeof(MorePlayers.MorePlayersMod), "MorePlayers", "1.0.11", "github.com/zxzinn")]
[assembly: MelonGame("ReLUGames", "MIMESIS")]

namespace MorePlayers
{
    public class MorePlayersMod : MelonMod
    {
        public const int MAX_PLAYERS = 999;

        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("=================================================");
            MelonLogger.Msg("MorePlayers Mod v1.0.11 - Verbose Debug Edition");
            MelonLogger.Msg("=================================================");
            MelonLogger.Msg("Author: github.com/zxzinn (based on Rxflex's work)");
            MelonLogger.Msg($"Max Players: {MAX_PLAYERS}");
            MelonLogger.Msg("");
            MelonLogger.Msg("Applying patches with detailed logging...");

            var harmony = new HarmonyLib.Harmony("com.moreplayers.mod");
            harmony.PatchAll(typeof(MorePlayersMod).Assembly);

            MelonLogger.Msg("=================================================");
            MelonLogger.Msg("All patches applied successfully!");
            MelonLogger.Msg("=================================================");
        }
    }

    // PATCH 1: GetMaximumClients - Prefix
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
                    MelonLogger.Msg("[PATCH 1] GetMaximumClients() - Prefix: FOUND");

                return method;
            }
            catch { return null; }
        }

        static bool Prefix(ref int __result)
        {
            __result = MorePlayersMod.MAX_PLAYERS;
            MelonLogger.Msg($"[PATCH 1] GetMaximumClients() called -> returning {__result}");
            return false;
        }
    }

    // PATCH 2: SetMaximumClients - Prefix
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
                    MelonLogger.Msg("[PATCH 2] SetMaximumClients() - Prefix: FOUND");

                return method;
            }
            catch { return null; }
        }

        static bool Prefix(ref int value)
        {
            int originalValue = value;
            if (value < MorePlayersMod.MAX_PLAYERS)
            {
                value = MorePlayersMod.MAX_PLAYERS;
                MelonLogger.Msg($"[PATCH 2] SetMaximumClients({originalValue}) -> changed to {value}");
            }
            else
            {
                MelonLogger.Msg($"[PATCH 2] SetMaximumClients({value}) -> no change needed");
            }
            return true;
        }
    }

    // PATCH 3: ServerSocket Constructor - Postfix
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
                    MelonLogger.Msg("[PATCH 3] Constructor - Postfix: FOUND");

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
                MelonLogger.Msg($"[PATCH 3] ServerSocket constructed -> SetMaximumClients({MorePlayersMod.MAX_PLAYERS})");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[PATCH 3] Error in postfix: {ex.Message}");
            }
        }
    }

    // PATCH 9: CreateLobby - Direct interception with lobby info readback
    [HarmonyPatch(typeof(SteamInviteDispatcher), "CreateLobby")]
    public class SteamLobbyCreation_Patch
    {
        static bool Prefix(bool isOpenForRandomMatch)
        {
            try
            {
                MelonLogger.Msg("========================================");
                MelonLogger.Msg("[PATCH 9] CreateLobby() called!");
                MelonLogger.Msg($"[PATCH 9] isOpenForRandomMatch = {isOpenForRandomMatch}");

                var steamMatchmakingType = Type.GetType("Steamworks.SteamMatchmaking, com.rlabrecque.steamworks.net");
                var eLobbyTypeType = Type.GetType("Steamworks.ELobbyType, com.rlabrecque.steamworks.net");
                var playerPrefsType = Type.GetType("UnityEngine.PlayerPrefs, UnityEngine.CoreModule");

                if (steamMatchmakingType == null || eLobbyTypeType == null || playerPrefsType == null)
                {
                    MelonLogger.Error("[PATCH 9] Failed to get required types");
                    MelonLogger.Error($"  steamMatchmakingType: {(steamMatchmakingType != null ? "OK" : "NULL")}");
                    MelonLogger.Error($"  eLobbyTypeType: {(eLobbyTypeType != null ? "OK" : "NULL")}");
                    MelonLogger.Error($"  playerPrefsType: {(playerPrefsType != null ? "OK" : "NULL")}");
                    return true;
                }

                var createLobbyMethod = steamMatchmakingType.GetMethod("CreateLobby",
                    BindingFlags.Public | BindingFlags.Static);
                var setIntMethod = playerPrefsType.GetMethod("SetInt",
                    BindingFlags.Public | BindingFlags.Static);

                if (createLobbyMethod == null || setIntMethod == null)
                {
                    MelonLogger.Error("[PATCH 9] Failed to get required methods");
                    MelonLogger.Error($"  CreateLobby: {(createLobbyMethod != null ? "OK" : "NULL")}");
                    MelonLogger.Error($"  SetInt: {(setIntMethod != null ? "OK" : "NULL")}");
                    return true;
                }

                // ELobbyType.FriendsOnly = 2
                var friendsOnly = Enum.ToObject(eLobbyTypeType, 2);

                MelonLogger.Msg($"[PATCH 9] Calling SteamMatchmaking.CreateLobby(");
                MelonLogger.Msg($"[PATCH 9]   eLobbyType = {friendsOnly} (FriendsOnly),");
                MelonLogger.Msg($"[PATCH 9]   cMaxMembers = {MorePlayersMod.MAX_PLAYERS}");
                MelonLogger.Msg($"[PATCH 9] )");

                createLobbyMethod.Invoke(null, new object[] { friendsOnly, MorePlayersMod.MAX_PLAYERS });
                setIntMethod.Invoke(null, new object[] { "TempLobbyIsOpen", isOpenForRandomMatch ? 1 : 0 });

                MelonLogger.Msg($"[PATCH 9] ✓ Steam lobby created successfully!");
                MelonLogger.Msg($"[PATCH 9]   Max members set to: {MorePlayersMod.MAX_PLAYERS}");
                MelonLogger.Msg($"[PATCH 9]   Original game would have used: 4");
                MelonLogger.Msg("========================================");

                return false; // Skip original method
            }
            catch (Exception ex)
            {
                MelonLogger.Error("========================================");
                MelonLogger.Error($"[PATCH 9] EXCEPTION: {ex.Message}");
                MelonLogger.Error($"[PATCH 9] Stack trace: {ex.StackTrace}");
                MelonLogger.Error("========================================");
                return true; // Run original if patch fails
            }
        }
    }

    // Additional patch: Monitor Steam lobby member limit
    [HarmonyPatch]
    public class SteamLobby_GetLobbyMemberLimit_Patch
    {
        static MethodBase TargetMethod()
        {
            try
            {
                var steamMatchmakingType = Type.GetType("Steamworks.SteamMatchmaking, com.rlabrecque.steamworks.net");
                var method = steamMatchmakingType?.GetMethod("GetLobbyMemberLimit",
                    BindingFlags.Public | BindingFlags.Static);

                if (method != null)
                    MelonLogger.Msg("[PATCH 10] GetLobbyMemberLimit() - Monitor: FOUND");
                else
                    MelonLogger.Warning("[PATCH 10] GetLobbyMemberLimit() - NOT FOUND");

                return method;
            }
            catch { return null; }
        }

        static void Postfix(ref int __result)
        {
            MelonLogger.Msg($"[PATCH 10] GetLobbyMemberLimit() returned: {__result}");
        }
    }
}
