using System;
using HarmonyLib;
using RoboticInbox.Utilities;

namespace RoboticInbox.Patches;

[HarmonyPatch(typeof(GameManager), "TEUnlockServer")]
internal class GameManager_TEUnlockServer_Patches
{
	private static readonly ModLog<GameManager_TEUnlockServer_Patches> _log = new ModLog<GameManager_TEUnlockServer_Patches>();

	public static void Postfix(int _clrIdx, Vector3i _blockPos)
	{
		try
		{
			if (SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer)
			{
				StorageManager.Distribute(_clrIdx, _blockPos);
			}
		}
		catch (Exception e)
		{
			_log.Error("Postfix", e);
		}
	}
}
