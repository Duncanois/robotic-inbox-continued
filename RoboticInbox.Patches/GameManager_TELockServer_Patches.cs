using System;
using HarmonyLib;
using RoboticInbox.Utilities;
using UnityEngine;

namespace RoboticInbox.Patches;

[HarmonyPatch(typeof(GameManager), "TELockServer")]
internal class GameManager_TELockServer_Patches
{
	private static readonly ModLog<GameManager_TELockServer_Patches> _log = new ModLog<GameManager_TELockServer_Patches>();

	public static bool Prefix(GameManager __instance, int _clrIdx, Vector3i _blockPos, int _lootEntityId, int _entityIdThatOpenedIt, string _customUi = null)
	{
		try
		{
			if (!SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer)
			{
				return true;
			}
			TileEntity tileEntity = ((WorldBase)__instance.m_World).GetTileEntity(_clrIdx, _blockPos);
			if (tileEntity == null)
			{
				return true;
			}
			BlockValue blockValue = tileEntity.blockValue;
			if (!StorageManager.HasRoboticInboxSecureTag(blockValue.Block))
			{
				return true;
			}
			if (__instance.lockedTileEntities.ContainsKey((ITileEntity)(object)tileEntity))
			{
				_log.Trace($"[{_blockPos}] robotic inbox denied access to {_entityIdThatOpenedIt} because it was actively distributing contents");
				Entity entity = ((WorldBase)__instance.m_World).GetEntity(_entityIdThatOpenedIt);
				if (entity is EntityPlayerLocal)
				{
					__instance.TEDeniedAccessClient(_clrIdx, _blockPos, _lootEntityId, _entityIdThatOpenedIt);
				}
				return false;
			}
			if (StorageManager.ActiveCoroutines.TryGetValue(_blockPos, out var value))
			{
				_log.Warn($"active coroutine detected at {_blockPos}; stopping and removing it before player {_entityIdThatOpenedIt} accesses underlying robotic inbox.");
				StorageManager.ActiveCoroutines.Remove(_blockPos);
				ThreadManager.StopCoroutine(value);

				// Force update the inbox to reflect current state - fixes dupe bug
				StorageManager.ForceContainerRefresh(_clrIdx, _blockPos);
				return true;
			}
		}
		catch (Exception e)
		{
			_log.Error("Postfix", e);
		}
		return true;
	}
}
