using System;
using System.Reflection;
using HarmonyLib;
using RoboticInbox.Utilities;

namespace RoboticInbox;

public class ModApi : IModApi
{
    private const string MOD_MAINTAINER = "kanaverum";
    private const string SUPPORT_LINK = "https://discord.gg/hYa2sNHXya";
    private const string DLL_VERSION = "dev-dll-version";
    private const string BUILD_TARGET = "dev-build-target";
    private static readonly ModLog<ModApi> _log = new ModLog<ModApi>();
    public static bool DebugMode { get; set; } = false;

    public void InitMod(Mod _modInstance)
    {
        try
        {
            _log.Info($"Robotic Inbox DLL Version {DLL_VERSION} build for 7DTD {BUILD_TARGET}");
            new Harmony(GetType().ToString()).PatchAll(Assembly.GetExecutingAssembly());
            SettingsManager.Load();
            // Remove broken ModEvents usage
            OnGameStartDone();
        }
        catch (Exception e)
        {
            _log.Error($"Failed to start up Robotic Inbox mod; take a look at logs for guidance but feel free to also reach out to the mod maintainer {MOD_MAINTAINER} via {SUPPORT_LINK}", e);
        }
    }

    private void OnGameStartDone()
    {
        try
        {
            StorageManager.OnGameStartDone();
            SignManager.OnGameStartDone();
        }
        catch (Exception e)
        {
            _log.Error("OnGameStartDone Failed", e);
        }
    }

    private void OnPlayerSpawnedInWorld(ClientInfo clientInfo, RespawnType respawnType, Vector3i pos)
    {
        try
        {
            EntityPlayer value;
            if (clientInfo == null)
            {
                if ((int)respawnType <= 2)
                {
                    var players = ((WorldBase)GameManager.Instance.World).GetLocalPlayers();
                    for (int i = 0; i < players.Count; i++)
                    {
                        SettingsManager.PropagateHorizontalRange((EntityPlayer)players[i]);
                        SettingsManager.PropagateVerticalRange((EntityPlayer)players[i]);
                    }
                }
            }
            else if (GameManager.Instance.World.Players.dict.TryGetValue(clientInfo.entityId, out value) && ((Entity)value).IsAlive() && ((int)respawnType == 2 || ((int)respawnType - 4 <= 1)))
            {
                SettingsManager.PropagateHorizontalRange(value);
                SettingsManager.PropagateVerticalRange(value);
            }
        }
        catch (Exception e)
        {
            _log.Error("Failed to handle PlayerSpawnedInWorld event.", e);
        }
    }
}
