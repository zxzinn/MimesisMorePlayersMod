using MelonLoader;
using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Reflection.Emit;

[assembly: MelonInfo(typeof(MorePlayers.MorePlayersMod), "MorePlayers", "1.0.9", "github.com/zxzinn")]
[assembly: MelonGame("ReLUGames", "MIMESIS")]

namespace MorePlayers
{
    public class MorePlayersMod : MelonMod
    {
        public const int MAX_PLAYERS = 999;

        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("=================================================");
            MelonLogger.Msg("MorePlayers Mod v1.0.9 - Diagnostic Edition");
            MelonLogger.Msg("=================================================");
            MelonLogger.Msg("Author: github.com/zxzinn (based on Rxflex's work)");
            MelonLogger.Msg($"Max Players: {MAX_PLAYERS}");
            MelonLogger.Msg("");
            MelonLogger.Msg("Applying all patches with enhanced logging...");

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
            MelonLogger.Msg($"[PATCH 1] GetMaximumClients called, returning {__result}");
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
            }
            MelonLogger.Msg($"[PATCH 2] SetMaximumClients called: {originalValue} -> {value}");
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
                MelonLogger.Msg($"[PATCH 3] ServerSocket constructor called, set max to {MorePlayersMod.MAX_PLAYERS}");
            }
            catch { }
        }
    }

    // PATCH 4: OnRemoteConnectionState monitoring
    [HarmonyPatch]
    public class ServerSocket_OnRemoteConnectionState_Patch
    {
        static MethodBase TargetMethod()
        {
            try
            {
                var assembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp");
                var serverSocketType = assembly?.GetType("FishySteamworks.Server.ServerSocket");
                var method = serverSocketType?.GetMethod("OnRemoteConnectionState",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                if (method != null)
                    MelonLogger.Msg("[PATCH 4] OnRemoteConnectionState - Monitor: FOUND");

                return method;
            }
            catch { return null; }
        }

        static void Prefix(object __instance, object args)
        {
            try
            {
                var argsType = args.GetType();
                var m_infoField = argsType.GetField("m_info");
                var m_info = m_infoField.GetValue(args);
                var m_infoType = m_info.GetType();

                var m_eStateField = m_infoType.GetField("m_eState");
                var m_eState = m_eStateField.GetValue(m_info);

                var m_identityRemoteField = m_infoType.GetField("m_identityRemote");
                var m_identityRemote = m_identityRemoteField.GetValue(m_info);
                var getSteamID64Method = m_identityRemote.GetType().GetMethod("GetSteamID64");
                var steamID = getSteamID64Method.Invoke(m_identityRemote, null);

                // Get current connection count
                var instanceType = __instance.GetType();
                var _steamConnectionsField = instanceType.GetField("_steamConnections",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                var _steamConnections = _steamConnectionsField.GetValue(__instance);
                var countProp = _steamConnections.GetType().GetProperty("Count");
                var currentCount = countProp.GetValue(_steamConnections);

                MelonLogger.Msg($"[PATCH 4] Connection event: SteamID={steamID}, State={m_eState}, CurrentConnections={currentCount}");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[PATCH 4] Monitoring error: {ex.Message}");
            }
        }
    }

    // PATCH 6: GetMemberCount - Return 0 to bypass >= 4 check
    [HarmonyPatch]
    public class GetMemberCount_Patch
    {
        static MethodBase TargetMethod()
        {
            try
            {
                var assembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp");
                var vWaitingRoomType = assembly?.GetType("VWaitingRoom");
                var method = vWaitingRoomType?.GetMethod("GetMemberCount",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                if (method != null)
                    MelonLogger.Msg("[PATCH 6] GetMemberCount() - Return 0: FOUND");
                else
                    MelonLogger.Warning("[PATCH 6] GetMemberCount() - NOT FOUND (might not exist in this version)");

                return method;
            }
            catch { return null; }
        }

        static bool Prefix(ref int __result)
        {
            __result = 0; // Return 0 to bypass "if (count >= 4)" check
            MelonLogger.Msg($"[PATCH 6] GetMemberCount called, returning 0 to bypass checks");
            return false;
        }
    }

    // PATCH 9: CreateLobby - Direct interception
    [HarmonyPatch(typeof(SteamInviteDispatcher), "CreateLobby")]
    public class SteamLobbyCreation_Patch
    {
        static bool Prefix(bool isOpenForRandomMatch)
        {
            try
            {
                MelonLogger.Msg("[PATCH 9] CreateLobby intercepted");

                var steamMatchmakingType = Type.GetType("Steamworks.SteamMatchmaking, com.rlabrecque.steamworks.net");
                var eLobbyTypeType = Type.GetType("Steamworks.ELobbyType, com.rlabrecque.steamworks.net");
                var playerPrefsType = Type.GetType("UnityEngine.PlayerPrefs, UnityEngine.CoreModule");

                if (steamMatchmakingType == null || eLobbyTypeType == null || playerPrefsType == null)
                {
                    MelonLogger.Error("[PATCH 9] Failed to get required types");
                    return true;
                }

                var createLobbyMethod = steamMatchmakingType.GetMethod("CreateLobby",
                    BindingFlags.Public | BindingFlags.Static);
                var setIntMethod = playerPrefsType.GetMethod("SetInt",
                    BindingFlags.Public | BindingFlags.Static);

                if (createLobbyMethod == null || setIntMethod == null)
                {
                    MelonLogger.Error("[PATCH 9] Failed to get required methods");
                    return true;
                }

                // ELobbyType.FriendsOnly = 2
                var friendsOnly = Enum.ToObject(eLobbyTypeType, 2);
                createLobbyMethod.Invoke(null, new object[] { friendsOnly, MorePlayersMod.MAX_PLAYERS });
                setIntMethod.Invoke(null, new object[] { "TempLobbyIsOpen", isOpenForRandomMatch ? 1 : 0 });

                MelonLogger.Msg($"[PATCH 9] Steam lobby created with {MorePlayersMod.MAX_PLAYERS} slots!");

                return false;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[PATCH 9] Failed: {ex.Message}");
                return true;
            }
        }
    }

    // PATCH 11a: UI Crash Prevention - SetPingImage
    [HarmonyPatch]
    public class InGameMenu_SetPingImage_Patch
    {
        static MethodBase TargetMethod()
        {
            try
            {
                var assembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp");
                var uiType = assembly?.GetType("UIPrefab_InGameMenu");
                var method = uiType?.GetMethod("SetPingImage",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                if (method != null)
                    MelonLogger.Msg("[PATCH 11a] SetPingImage - UI Protection: FOUND");

                return method;
            }
            catch { return null; }
        }

        static Exception Finalizer(Exception __exception)
        {
            if (__exception != null && __exception is ArgumentOutOfRangeException)
            {
                MelonLogger.Msg("[PATCH 11a] Suppressed UI array bounds error in SetPingImage");
                return null;
            }
            return __exception;
        }
    }

    // PATCH 11b: UI Crash Prevention - InitializePlayerUI
    [HarmonyPatch]
    public class InGameMenu_InitPlayerUI_Patch
    {
        static MethodBase TargetMethod()
        {
            try
            {
                var assembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp");
                var uiType = assembly?.GetType("UIPrefab_InGameMenu");
                var method = uiType?.GetMethod("InitializePlayerUI",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                if (method != null)
                    MelonLogger.Msg("[PATCH 11b] InitializePlayerUI - UI Protection: FOUND");

                return method;
            }
            catch { return null; }
        }

        static Exception Finalizer(Exception __exception)
        {
            if (__exception != null && __exception is ArgumentOutOfRangeException)
            {
                MelonLogger.Msg("[PATCH 11b] Suppressed UI array bounds error in InitializePlayerUI");
                return null;
            }
            return __exception;
        }
    }

    // PATCH 11c: UI Crash Prevention - SurvivalResult
    [HarmonyPatch]
    public class SurvivalResult_PatchParameter_Patch
    {
        static MethodBase TargetMethod()
        {
            try
            {
                var assembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp");
                var uiType = assembly?.GetType("UIPrefab_SurvivalResult");
                var method = uiType?.GetMethod("PatchParameter",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                if (method != null)
                    MelonLogger.Msg("[PATCH 11c] PatchParameter - UI Protection: FOUND");

                return method;
            }
            catch { return null; }
        }

        static Exception Finalizer(Exception __exception)
        {
            if (__exception != null && __exception is ArgumentOutOfRangeException)
            {
                MelonLogger.Msg("[PATCH 11c] Suppressed UI array bounds error in SurvivalResult");
                return null;
            }
            return __exception;
        }
    }
}
