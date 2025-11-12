using MelonLoader;
using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;

[assembly: MelonInfo(typeof(MorePlayers.MorePlayersMod), "MorePlayers", "1.0.7", "github.com/zxzinn")]
[assembly: MelonGame("ReLUGames", "MIMESIS")]

namespace MorePlayers
{
    public class MorePlayersMod : MelonMod
    {
        public const int MAX_PLAYERS = 250; // Steam's actual limit

        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("=================================================");
            MelonLogger.Msg("MorePlayers Mod v1.0.7 - Fixed for EA0.2.5+");
            MelonLogger.Msg("=================================================");
            MelonLogger.Msg("Author: github.com/zxzinn (based on Rxflex's work)");
            MelonLogger.Msg($"Max Players: {MAX_PLAYERS} (Steam's hard limit)");
            MelonLogger.Msg("");

            var harmony = new HarmonyLib.Harmony("com.moreplayers.mod");
            harmony.PatchAll(typeof(MorePlayersMod).Assembly);

            MelonLogger.Msg("=================================================");
            MelonLogger.Msg("All patches applied successfully!");
            MelonLogger.Msg("=================================================");
        }
    }

    // Patch the CreateLobby method to use 250 players instead of 4
    [HarmonyPatch(typeof(SteamInviteDispatcher), "CreateLobby")]
    public class SteamLobbyCreation_Patch
    {
        static bool Prefix(bool isOpenForRandomMatch)
        {
            try
            {
                // Get Steamworks types via reflection
                var steamMatchmakingType = Type.GetType("Steamworks.SteamMatchmaking, com.rlabrecque.steamworks.net");
                var eLobbyTypeType = Type.GetType("Steamworks.ELobbyType, com.rlabrecque.steamworks.net");
                var playerPrefsType = Type.GetType("UnityEngine.PlayerPrefs, UnityEngine.CoreModule");

                if (steamMatchmakingType == null || eLobbyTypeType == null || playerPrefsType == null)
                {
                    MelonLogger.Error("[MorePlayers] Failed to get required types");
                    return true;
                }

                // Get the CreateLobby method
                var createLobbyMethod = steamMatchmakingType.GetMethod("CreateLobby",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

                // Get PlayerPrefs.SetInt method
                var setIntMethod = playerPrefsType.GetMethod("SetInt",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

                if (createLobbyMethod == null || setIntMethod == null)
                {
                    MelonLogger.Error("[MorePlayers] Failed to get required methods");
                    return true;
                }

                // ELobbyType.FriendsOnly = 2
                var friendsOnly = Enum.ToObject(eLobbyTypeType, 2);

                // Call SteamMatchmaking.CreateLobby with MAX_PLAYERS
                createLobbyMethod.Invoke(null, new object[] { friendsOnly, MorePlayersMod.MAX_PLAYERS });

                // Set PlayerPrefs for random match flag
                setIntMethod.Invoke(null, new object[] { "TempLobbyIsOpen", isOpenForRandomMatch ? 1 : 0 });

                MelonLogger.Msg($"[MorePlayers] CreateLobby intercepted! Creating lobby with {MorePlayersMod.MAX_PLAYERS} slots (was 4)");

                return false; // Skip original method
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[MorePlayers] Failed to patch CreateLobby: {ex.Message}");
                MelonLogger.Error($"Stack trace: {ex.StackTrace}");
                return true; // Run original if patch fails
            }
        }
    }

    // Patch FishySteamworks ServerSocket SetMaximumClients
    [HarmonyPatch]
    public class ServerSocket_SetMaximumClients_Patch
    {
        static MethodBase TargetMethod()
        {
            try
            {
                var assembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp");

                if (assembly == null)
                {
                    MelonLogger.Error("[MorePlayers] Assembly-CSharp not found");
                    return null;
                }

                var serverSocketType = assembly.GetType("FishySteamworks.Server.ServerSocket");
                if (serverSocketType == null)
                {
                    MelonLogger.Warning("[MorePlayers] ServerSocket type not found");
                    return null;
                }

                var method = serverSocketType.GetMethod("SetMaximumClients",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                if (method != null)
                {
                    MelonLogger.Msg("[MorePlayers] Found SetMaximumClients method");
                }

                return method;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[MorePlayers] Error finding SetMaximumClients: {ex.Message}");
                return null;
            }
        }

        static bool Prefix(ref int value)
        {
            if (value < MorePlayersMod.MAX_PLAYERS)
            {
                MelonLogger.Msg($"[MorePlayers] SetMaximumClients: changing {value} to {MorePlayersMod.MAX_PLAYERS}");
                value = MorePlayersMod.MAX_PLAYERS;
            }
            return true;
        }
    }

    // Patch GetMaximumClients to return our value
    [HarmonyPatch]
    public class ServerSocket_GetMaximumClients_Patch
    {
        static MethodBase TargetMethod()
        {
            try
            {
                var assembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp");

                if (assembly == null) return null;

                var serverSocketType = assembly.GetType("FishySteamworks.Server.ServerSocket");
                if (serverSocketType == null) return null;

                return serverSocketType.GetMethod("GetMaximumClients",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            }
            catch
            {
                return null;
            }
        }

        static bool Prefix(ref int __result)
        {
            __result = MorePlayersMod.MAX_PLAYERS;
            return false; // Skip original
        }
    }
}
