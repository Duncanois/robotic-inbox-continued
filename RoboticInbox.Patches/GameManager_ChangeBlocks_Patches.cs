using System;
using System.Collections.Generic;
using HarmonyLib;
using RoboticInbox.Utilities;

namespace RoboticInbox.Patches;

[HarmonyPatch(typeof(GameManager), "ChangeBlocks")]
internal class GameManager_ChangeBlocks_Patches
{
	private static readonly ModLog<GameManager_ChangeBlocks_Patches> _log = new ModLog<GameManager_ChangeBlocks_Patches>();

	private static bool Prefix(PlatformUserIdentifierAbs persistentPlayerId, ref List<BlockChangeInfo> _blocksToChange, GameManager __instance)
	{
		try
		{
			if (!SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer)
			{
				return true;
			}
			for (int i = 0; i < _blocksToChange.Count; i++)
			{
				BlockChangeInfo val = _blocksToChange[i];
				// FIX: Remove invalid 'ref' usage, use direct access
				if (val.blockValue.ischild || val.blockValue.isair || !val.bChangeBlockValue || !StorageManager.HasRoboticInboxSecureTag(val.blockValue.Block))
				{
					continue;
				}
				TileEntity tileEntity = ((WorldBase)__instance.m_World).GetTileEntity(val.pos);
				if (tileEntity == null)
				{
					continue;
				}
				TileEntityComposite val2 = (TileEntityComposite)(object)((tileEntity is TileEntityComposite) ? tileEntity : null);
				if (val2 == null)
				{
					continue;
				}
				BlockValue blockValue = tileEntity.blockValue;
				if (!StorageManager.HasRepairableLockTag(blockValue.Block))
				{
					continue;
				}
				if (persistentPlayerId == null)
				{
					ModLog<GameManager_ChangeBlocks_Patches> log = _log;
					object arg = val.pos;
					PersistentPlayerData persistentLocalPlayer = __instance.persistentLocalPlayer;
					object arg2;
					if (persistentLocalPlayer == null)
					{
						arg2 = null;
					}
					else
					{
						PlatformUserIdentifierAbs primaryId = persistentLocalPlayer.PrimaryId;
						arg2 = ((primaryId != null) ? primaryId.CombinedString : null);
					}
					log.Trace($"[{arg}] {arg2} repaired and has taken ownership over robotic inbox");
					PersistentPlayerData persistentLocalPlayer2 = __instance.persistentLocalPlayer;
					val2.Owner = ((persistentLocalPlayer2 != null) ? persistentLocalPlayer2.PrimaryId : null);
				}
				else
				{
					_log.Trace($"[{val.pos}] {persistentPlayerId.CombinedString} repaired and has taken ownership over robotic inbox");
					val2.Owner = persistentPlayerId;
				}
				((TileEntity)val2).SetModified();
			}
		}
		catch (Exception e)
		{
			_log.Error("Postfix", e);
		}
		return true;
	}
}
