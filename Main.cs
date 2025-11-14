using MelonLoader;
using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Net.Http;
using System.Threading.Tasks;

[assembly: MelonInfo(typeof(MorePlayers.MorePlayersMod), "MorePlayers", "1.8.0-zxzinn", "github.com/zxzinn")]
[assembly: MelonGame("ReLUGames", "MIMESIS")]

namespace MorePlayers
{
    public class MorePlayersMod : MelonMod
    {
        public const int MAX_PLAYERS = 999;
        private const string CURRENT_VERSION = "1.8.0-zxzinn";
        private const string GITHUB_API_URL = "https://api.github.com/repos/zxzinn/MimesisMorePlayers-Enhanced/releases/latest";
        private const string GITHUB_RELEASES_URL = "https://github.com/zxzinn/MimesisMorePlayers-Enhanced/releases";

        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("=================================================");
            MelonLogger.Msg($"MorePlayers Mod v{CURRENT_VERSION}");
            MelonLogger.Msg("=================================================");
            MelonLogger.Msg("Author: github.com/zxzinn");
            MelonLogger.Msg($"Max Players: {MAX_PLAYERS}");
            MelonLogger.Msg("");

            var harmony = new HarmonyLib.Harmony("com.moreplayers.mod");
            harmony.PatchAll(typeof(MorePlayersMod).Assembly);

            MelonLogger.Msg("");
            MelonLogger.Msg("All patches applied!");
            MelonLogger.Msg("=================================================");

            // Check for updates asynchronously
            Task.Run(CheckForUpdates);
        }

        private async Task CheckForUpdates()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "MorePlayers-Mod");
                    client.Timeout = TimeSpan.FromSeconds(5);

                    var response = await client.GetStringAsync(GITHUB_API_URL);

                    // Simple parsing: find "tag_name":"v1.x.x"
                    var tagStart = response.IndexOf("\"tag_name\":\"") + 12;
                    var tagEnd = response.IndexOf("\"", tagStart);
                    var latestVersion = response.Substring(tagStart, tagEnd - tagStart).TrimStart('v');

                    if (latestVersion != CURRENT_VERSION)
                    {
                        MelonLogger.Msg("");
                        MelonLogger.Msg("=================================================");
                        MelonLogger.Warning($"🔔 UPDATE AVAILABLE!");
                        MelonLogger.Warning($"   Current: v{CURRENT_VERSION}");
                        MelonLogger.Warning($"   Latest:  v{latestVersion}");
                        MelonLogger.Msg($"   Download: {GITHUB_RELEASES_URL}");
                        MelonLogger.Msg("=================================================");
                        MelonLogger.Msg("");
                    }
                    else
                    {
                        MelonLogger.Msg($"✓ You're using the latest version!");
                    }
                }
            }
            catch
            {
                // Silently fail if update check fails (no internet, API down, etc.)
            }
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
                    MelonLogger.Msg("[✓] ServerSocket.GetMaximumClients");

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
                    MelonLogger.Msg("[✓] ServerSocket.SetMaximumClients");

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
                    MelonLogger.Msg("[✓] ServerSocket Constructor");

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

    // PATCH 3b: ServerSocket - All methods transpiler to replace _maximumClients field reads
    [HarmonyPatch]
    public class ServerSocket_AllMethods_Transpiler
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            try
            {
                var assembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp");
                var serverSocketType = assembly?.GetType("FishySteamworks.Server.ServerSocket");

                if (serverSocketType == null)
                    return new MethodBase[0];

                var methods = serverSocketType.GetMethods(
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                    .Where(m => !m.IsAbstract && !m.IsConstructor && !m.IsGenericMethod)
                    .ToList();

                MelonLogger.Msg($"[✓] ServerSocket: Patching {methods.Count} methods with transpiler");
                return methods;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"ServerSocket transpiler error: {ex.Message}");
                return new MethodBase[0];
            }
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            try
            {
                // Get the _maximumClients field
                var assembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp");
                var serverSocketType = assembly?.GetType("FishySteamworks.Server.ServerSocket");
                var maxClientsField = serverSocketType?.GetField("_maximumClients",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                if (maxClientsField == null)
                    return codes;

                for (int i = 0; i < codes.Count; i++)
                {
                    // If loading the _maximumClients field
                    if (codes[i].opcode == OpCodes.Ldfld && codes[i].operand is System.Reflection.FieldInfo field &&
                        field.Name == "_maximumClients")
                    {
                        // Replace: Pop the field value and push MAX_PLAYERS
                        codes.InsertRange(i + 1, new[]
                        {
                            new CodeInstruction(OpCodes.Pop),
                            new CodeInstruction(OpCodes.Ldc_I4, MorePlayersMod.MAX_PLAYERS)
                        });
                        i += 2;
                    }
                }
            }
            catch { }

            return codes;
        }
    }

    // PATCH 3c: GetMemberCount - Smart return based on caller
    [HarmonyPatch]
    public class GetMemberCount_Smart_Patch
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            try
            {
                var assembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp");

                var methods = new List<MethodBase>();

                // Patch IVroom.GetMemberCount
                var ivroomType = assembly?.GetType("IVroom");
                var method = ivroomType?.GetMethod("GetMemberCount",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (method != null)
                {
                    methods.Add(method);
                    MelonLogger.Msg("[✓] IVroom.GetMemberCount (smart patch)");
                }

                return methods;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"GetMemberCount patch error: {ex.Message}");
                return new MethodBase[0];
            }
        }

        static bool Prefix(ref int __result, object __instance)
        {
            try
            {
                // Get the actual member count
                var vPlayerDictField = __instance.GetType().GetField("_vPlayerDict",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                var vPlayerDict = vPlayerDictField?.GetValue(__instance) as System.Collections.IDictionary;
                int actualCount = vPlayerDict?.Count ?? 0;

                // Check the call stack to determine context
                var stackTrace = new System.Diagnostics.StackTrace();
                bool isFromEnterCheck = false;
                bool isFromSessionCount = false;

                for (int i = 0; i < Math.Min(stackTrace.FrameCount, 10); i++)
                {
                    var frame = stackTrace.GetFrame(i);
                    var method = frame?.GetMethod();
                    if (method != null)
                    {
                        string methodName = method.Name;
                        // Check if called from Enter room methods
                        if (methodName.Contains("EnterWaitingRoom") ||
                            methodName.Contains("EnterMaintenenceRoom") ||
                            methodName.Contains("CanEnter"))
                        {
                            isFromEnterCheck = true;
                            break;
                        }
                        // Check if called from session count
                        if (methodName.Contains("GetSessionCount") ||
                            methodName.Contains("GetRoomMemberCount"))
                        {
                            isFromSessionCount = true;
                            break;
                        }
                    }
                }

                if (isFromEnterCheck)
                {
                    // For enter checks, return 0 to bypass 4-player limit
                    __result = 0;
                    return false;
                }
                else if (isFromSessionCount)
                {
                    // For session counting, return actual count
                    __result = actualCount;
                    return false;
                }

                // Default: return actual count
                __result = actualCount;
                return false;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"GetMemberCount Prefix error: {ex.Message}");
                return true; // Let original method run on error
            }
        }
    }

    // PATCH 4: All Room CanEnterChannel - Override to use correct player count check
    [HarmonyPatch]
    public class AllRooms_CanEnterChannel_Patch
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            try
            {
                var assembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp");

                var methods = new List<MethodBase>();

                // Patch VWaitingRoom.CanEnterChannel
                var vWaitingRoomType = assembly?.GetType("VWaitingRoom");
                var waitingMethod = vWaitingRoomType?.GetMethod("CanEnterChannel",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (waitingMethod != null)
                {
                    methods.Add(waitingMethod);
                    MelonLogger.Msg("[✓] VWaitingRoom.CanEnterChannel");
                }

                // Patch MaintenanceRoom.CanEnterChannel
                var maintenanceRoomType = assembly?.GetType("MaintenanceRoom");
                var maintenanceMethod = maintenanceRoomType?.GetMethod("CanEnterChannel",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (maintenanceMethod != null)
                {
                    methods.Add(maintenanceMethod);
                    MelonLogger.Msg("[✓] MaintenanceRoom.CanEnterChannel");
                }

                // Patch base IVroom.CanEnterChannel
                var ivroomType = assembly?.GetType("IVroom");
                var ivroomMethod = ivroomType?.GetMethod("CanEnterChannel",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (ivroomMethod != null)
                {
                    methods.Add(ivroomMethod);
                    MelonLogger.Msg("[✓] IVroom.CanEnterChannel");
                }

                return methods;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"CanEnterChannel patch error: {ex.Message}");
                return new MethodBase[0];
            }
        }

        static bool Prefix(ref object __result, object __instance, long playerUID)
        {
            try
            {
                // Get MsgErrorCode enum type
                var assembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name.Contains("FishySteamworks") || a.GetName().Name == "Assembly-CSharp");
                var msgErrorCodeType = assembly?.GetTypes().FirstOrDefault(t => t.Name == "MsgErrorCode");

                if (msgErrorCodeType == null || !msgErrorCodeType.IsEnum)
                    return true; // Let original method run

                // Check for duplicate player
                var vPlayerDictField = __instance.GetType().GetField("_vPlayerDict",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                var vPlayerDict = vPlayerDictField?.GetValue(__instance) as System.Collections.IDictionary;

                if (vPlayerDict != null)
                {
                    foreach (var player in vPlayerDict.Values)
                    {
                        var uidProp = player.GetType().GetProperty("UID",
                            BindingFlags.Public | BindingFlags.Instance);
                        if (uidProp != null)
                        {
                            var uid = uidProp.GetValue(player);
                            if (uid != null && uid.Equals(playerUID))
                            {
                                __result = Enum.Parse(msgErrorCodeType, "DuplicatePlayer");
                                return false; // Skip original method
                            }
                        }
                    }

                    // Check player count against MAX_PLAYERS instead of 4
                    if (vPlayerDict.Count >= MorePlayersMod.MAX_PLAYERS)
                    {
                        __result = Enum.Parse(msgErrorCodeType, "PlayerCountExceeded");
                        return false;
                    }
                }

                // Return Success
                __result = Enum.Parse(msgErrorCodeType, "Success");
                return false; // Skip original method
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"CanEnterChannel Prefix error: {ex.Message}");
                return true; // Let original method run on error
            }
        }
    }

    // PATCH 5: VRoomManager.EnterWaitingRoom - Use reflection to bypass check
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
                    MelonLogger.Msg("[✓] VRoomManager.EnterWaitingRoom");

                return method;
            }
            catch { return null; }
        }

        static void Prefix(object __instance, object context)
        {
            try
            {
                // Get VWaitingRoom
                var vroomsField = __instance.GetType().GetField("_vrooms", BindingFlags.NonPublic | BindingFlags.Instance);
                var vrooms = vroomsField?.GetValue(__instance) as System.Collections.IDictionary;

                if (vrooms != null)
                {
                    foreach (var room in vrooms.Values)
                    {
                        if (room.GetType().Name == "VWaitingRoom")
                        {
                            // Set _maxPlayers to MAX_PLAYERS
                            var maxPlayersField = room.GetType().BaseType?.GetField("_maxPlayers",
                                BindingFlags.NonPublic | BindingFlags.Instance);
                            if (maxPlayersField != null)
                            {
                                maxPlayersField.SetValue(room, MorePlayersMod.MAX_PLAYERS);
                            }
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"EnterWaitingRoom Prefix error: {ex.Message}");
            }
        }
    }

    // PATCH 5b: VRoomManager.EnterMaintenenceRoom - Use reflection to bypass check
    [HarmonyPatch]
    public class VRoomManager_EnterMaintenenceRoom_Patch
    {
        static MethodBase TargetMethod()
        {
            try
            {
                var assembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp");
                var vroomManagerType = assembly?.GetType("VRoomManager");
                var method = vroomManagerType?.GetMethod("EnterMaintenenceRoom",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                if (method != null)
                    MelonLogger.Msg("[✓] VRoomManager.EnterMaintenenceRoom");

                return method;
            }
            catch { return null; }
        }

        static void Prefix(object __instance, object context)
        {
            try
            {
                // Get MaintenanceRoom
                var vroomsField = __instance.GetType().GetField("_vrooms", BindingFlags.NonPublic | BindingFlags.Instance);
                var vrooms = vroomsField?.GetValue(__instance) as System.Collections.IDictionary;

                if (vrooms != null)
                {
                    foreach (var room in vrooms.Values)
                    {
                        if (room.GetType().Name == "MaintenanceRoom")
                        {
                            // Set _maxPlayers to MAX_PLAYERS
                            var maxPlayersField = room.GetType().BaseType?.GetField("_maxPlayers",
                                BindingFlags.NonPublic | BindingFlags.Instance);
                            if (maxPlayersField != null)
                            {
                                maxPlayersField.SetValue(room, MorePlayersMod.MAX_PLAYERS);
                            }
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"EnterMaintenenceRoom Prefix error: {ex.Message}");
            }
        }
    }

    // PATCH 6: GameSessionInfo.AddPlayerSteamID
    [HarmonyPatch]
    public class GameSessionInfo_AddPlayerSteamID_Patch
    {
        static MethodBase TargetMethod()
        {
            try
            {
                var assembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp");
                var type = assembly?.GetType("GameSessionInfo");
                var method = type?.GetMethod("AddPlayerSteamID",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                if (method != null)
                    MelonLogger.Msg("[✓] GameSessionInfo.AddPlayerSteamID");

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
                }
            }

            return codes;
        }
    }

    // PATCH 7: CreateLobby
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

                MelonLogger.Msg($"[✓] CreateLobby: {MorePlayersMod.MAX_PLAYERS} slots");

                var friendsOnly = Enum.ToObject(eLobbyTypeType, 2);
                createLobbyMethod.Invoke(null, new object[] { friendsOnly, MorePlayersMod.MAX_PLAYERS });
                setIntMethod.Invoke(null, new object[] { "TempLobbyIsOpen", isOpenForRandomMatch ? 1 : 0 });

                return false;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"CreateLobby patch error: {ex.Message}");
                return true;
            }
        }
    }
}
