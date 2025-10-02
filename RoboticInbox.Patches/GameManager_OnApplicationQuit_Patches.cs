using System;
using HarmonyLib;
using RoboticInbox.Utilities;

namespace RoboticInbox.Patches;

[HarmonyPatch(typeof(GameManager), "OnApplicationQuit")]
internal class GameManager_OnApplicationQuit_Patches
{
	private static readonly ModLog<GameManager_OnApplicationQuit_Patches> _log = new ModLog<GameManager_OnApplicationQuit_Patches>();

	public static bool Prefix()
	{
		try
		{
			StorageManager.OnGameManagerApplicationQuit();
			SignManager.OnGameManagerApplicationQuit();
		}
		catch (Exception e)
		{
			_log.Error("OnGameShutdown Failed", e);
		}
		return true;
	}
}
