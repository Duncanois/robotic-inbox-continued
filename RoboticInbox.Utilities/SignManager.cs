using System;
using System.Collections;
using System.Collections.Concurrent;
using UnityEngine;

namespace RoboticInbox.Utilities;

internal class SignManager
{
	private static readonly ModLog<SignManager> _log = new ModLog<SignManager>();

	private static Coroutine _signManagerCoroutine;

	private static ConcurrentDictionary<Vector3i, DateTime> MessageExpirations { get; } = new ConcurrentDictionary<Vector3i, DateTime>();

	private static ConcurrentDictionary<Vector3i, string> OriginalMessages { get; } = new ConcurrentDictionary<Vector3i, string>();

	internal static void OnGameStartDone()
	{
		_signManagerCoroutine = ThreadManager.StartCoroutine(MonitorContainerSigns());
		_log.Info("[OnGameStartDone] SignManager MonitorContainerSigns coroutine started.");
	}

	internal static void OnGameManagerApplicationQuit()
	{
		if (_signManagerCoroutine != null)
		{
			ThreadManager.StopCoroutine(_signManagerCoroutine);
			_log.Info("[OnGameManagerApplicationQuit] SignManager coroutine stopped.");
		}
		RestoreAllMessagesBeforeShutdown(GameManager.Instance.World);
	}

	internal static void HandleTargetLockedWithoutPassword(Vector3i pos, TileEntity target)
	{
		if (TryCastToITileEntitySignable(target, out var signable) && TryGetOwner(target, out var owner))
		{
			ShowTemporaryText(pos, SettingsManager.DistributionBlockedNoticeTime, signable, owner, "Can't Distribute: Container Locked without password");
		}
		NotificationManager.PlaySoundVehicleStorageOpen(new Vector3(pos.x, pos.y, pos.z));
	}

	internal static void HandleTargetLockedWhileSourceIsNot(Vector3i pos, TileEntity target)
	{
		if (TryCastToITileEntitySignable(target, out var signable) && TryGetOwner(target, out var owner))
		{
			ShowTemporaryText(pos, SettingsManager.DistributionBlockedNoticeTime, signable, owner, "Can't Distribute: Container Locked but Inbox is not");
		}
		NotificationManager.PlaySoundVehicleStorageOpen(new Vector3(pos.x, pos.y, pos.z));
	}

	internal static void HandlePasswordMismatch(Vector3i pos, TileEntity target)
	{
		if (TryCastToITileEntitySignable(target, out var signable) && TryGetOwner(target, out var owner))
		{
			ShowTemporaryText(pos, SettingsManager.DistributionBlockedNoticeTime, signable, owner, "Can't Distribute: Password Does not match Inbox");
		}
		NotificationManager.PlaySoundVehicleStorageOpen(new Vector3(pos.x, pos.y, pos.z));
	}

	internal static void HandleTransferred(Vector3i pos, TileEntity target, int totalItemsTransferred)
	{
		if (TryCastToITileEntitySignable(target, out var signable) && TryGetOwner(target, out var owner))
		{
			ShowTemporaryText(
				pos,
				SettingsManager.DistributionSuccessNoticeTime,
				signable,
				owner,
				string.Format("Added + Sorted\n{0} Item{1}", totalItemsTransferred, (totalItemsTransferred > 1) ? "s" : "")
			);
		}
		NotificationManager.PlaySoundVehicleStorageClose(new Vector3(pos.x, pos.y, pos.z));
	}

	private static IEnumerator MonitorContainerSigns()
	{
		// reduced verbosity: only log start as Info; loop is quiet
		_log.Info("[MonitorContainerSigns] Started monitor loop.");
		// production: 3s frequency to reduce CPU/log noise
		while (true)
		{
			// Re-fetch the world each iteration to avoid using a stale/null reference captured at coroutine start
			var world = GameManager.Instance?.World;
			RestoreExpiredMessages(world);
			yield return (object)new WaitForSeconds(3.0f);
		}
	}

	private static bool TryCastToITileEntitySignable(TileEntity tileEntity, out ITileEntitySignable signable)
	{
		TileEntitySecureLootContainerSigned val = (TileEntitySecureLootContainerSigned)(object)((tileEntity is TileEntitySecureLootContainerSigned) ? tileEntity : null);
		if (val == null)
		{
			TileEntityComposite val2 = (TileEntityComposite)(object)((tileEntity is TileEntityComposite) ? tileEntity : null);
			if (val2 != null)
			{
				signable = (ITileEntitySignable)(object)val2.GetFeature<TEFeatureSignable>();
				return signable != null;
			}
			signable = null;
			return false;
		}
		signable = (ITileEntitySignable)(object)val;
		return true;
	}

	private static bool TryGetOwner(TileEntity target, out PlatformUserIdentifierAbs owner)
	{
		TileEntityComposite val = (TileEntityComposite)(object)((target is TileEntityComposite) ? target : null);
		if (val == null)
		{
			TileEntitySecureLootContainerSigned val2 = (TileEntitySecureLootContainerSigned)(object)((target is TileEntitySecureLootContainerSigned) ? target : null);
			if (val2 != null)
			{
				owner = ((TileEntitySecureLootContainer)val2).ownerID;
				return true;
			}
			owner = null;
			return false;
		}
		owner = val.Owner;
		return true;
	}

	private static void ShowTemporaryText(Vector3i pos, float seconds, ITileEntitySignable signableEntity, PlatformUserIdentifierAbs signingPlayer, string text)
	{
		if (signingPlayer == null)
		{
			_log.Error($"[{pos}] no signing player found on target container; cannot update with info text");
			return;
		}

		string originalText = null;
		try
		{
			originalText = signableEntity.GetAuthoredText()?.Text ?? string.Empty;
		}
		catch (Exception ex)
		{
			_log.Warn($"[{pos}] failed to read authored text from signable: {ex.Message}", ex);
			originalText = string.Empty;
		}

		// keep a concise debug line to show what was written and original length
		_log.Debug($"[{pos}] ShowTemporaryText: tempText=\"{(text ?? "").Replace("\n", "\\n")}\", seconds={seconds}, originalLength={(originalText?.Length ?? 0)}");

		// Only store the original message if it is not already present
		if (!OriginalMessages.ContainsKey(pos))
		{
			OriginalMessages.TryAdd(pos, originalText);
		}

		// Set expiration (concise log)
		var expiry = DateTime.Now.AddSeconds(seconds);
		MessageExpirations.TryRemove(pos, out var _old);
		MessageExpirations.TryAdd(pos, expiry);
		_log.Debug($"[{pos}] Temporary message scheduled to expire at {expiry:O} (tracked={MessageExpirations.Count})");

		signableEntity.SetText(text, true, signingPlayer);
	}

	private static void RestoreExpiredMessages(World world)
	{
		DateTime now = DateTime.Now;
		_log.Debug($"[RestoreExpiredMessages] Checking for expired messages at {now}; tracked={MessageExpirations.Count}");
		foreach (Vector3i key in MessageExpirations.Keys)
		{
			if (MessageExpirations.TryGetValue(key, out var value) && value < now)
			{
				_log.Debug($"[RestoreExpiredMessages] Expired message for {key} at {value}");
				if (!MessageExpirations.TryRemove(key, out var _))
				{
					_log.Warn($"[{key}] failed to remove expiration entry before restore; continuing.");
				}

				if (TryRestoreOriginalMessage(world, key, broadcastToPlayers: true))
				{
					_log.Info($"[{key}] restored original message to container.");
				}
				else
				{
					_log.Warn($"[{key}] failed to restore original message to container; scheduling retry.");
					// transient failure: schedule a short retry/backoff
					var retryAt = DateTime.Now.AddSeconds(5.0);
					if (MessageExpirations.TryAdd(key, retryAt))
					{
						_log.Debug($"[{key}] scheduled retry at {retryAt:O}");
					}
				}
			}
		}
	}

	private static void RestoreAllMessagesBeforeShutdown(World world)
	{
		MessageExpirations.Clear();
		foreach (Vector3i key in OriginalMessages.Keys)
		{
			if (TryRestoreOriginalMessage(world, key, broadcastToPlayers: false))
			{
				_log.Info($"Successfully restored original message to container at {key} during shutdown.");
			}
			else
			{
				_log.Info($"Failed to restore original message to container at {key} during shutdown.");
			}
		}
	}

	private static bool TryRestoreOriginalMessage(World world, Vector3i pos, bool broadcastToPlayers)
	{
		_log.Debug($"[TryRestoreOriginalMessage] Enter for {pos}, broadcast={broadcastToPlayers}");

		// Read original without removing so we don't lose it if restore fails
		if (!OriginalMessages.TryGetValue(pos, out var value))
		{
			_log.Warn($"[TryRestoreOriginalMessage] No original message found for {pos}");
			return false;
		}

		// Validate world / tile entity - re-fetch GameManager.World if caller passed null
		if (world == null)
		{
			world = GameManager.Instance?.World;
			if (world == null)
			{
				_log.Warn($"[TryRestoreOriginalMessage] World is null for {pos}");
				return false;
			}
		}

		TileEntity tileEntity = ((WorldBase)world).GetTileEntity(pos);
		if (tileEntity == null)
		{
			_log.Warn($"[TryRestoreOriginalMessage] No tile entity found at {pos}");
			return false;
		}

		// Resolve owner and signable
		if (!TryGetOwner(tileEntity, out var owner))
		{
			_log.Warn($"[TryRestoreOriginalMessage] No owner found for tile entity at {pos}");
			return false;
		}

		if (!TryCastToITileEntitySignable(tileEntity, out var signable))
		{
			_log.Warn($"[TryRestoreOriginalMessage] No signable feature found for tile entity at {pos}");
			return false;
		}

		try
		{
			signable.SetText(value, broadcastToPlayers, owner);

			// Force server sync and read back to confirm
			try
			{
				((TileEntity)tileEntity).SetModified();
			}
			catch (Exception exMod)
			{
				_log.Warn($"[{pos}] SetModified threw: {exMod.Message}", exMod);
			}

			// Only now remove the stored original (restore succeeded)
			if (!OriginalMessages.TryRemove(pos, out var _removed))
			{
				_log.Warn($"[{pos}] Unexpected: failed to remove original message after successful restore.");
			}

			return true;
		}
		catch (Exception ex)
		{
			_log.Error($"[TryRestoreOriginalMessage] Unexpected exception for {pos}: {ex.Message}", ex);
			// keep the original in the dictionary so another attempt can run later
			return false;
		}
	}

	// added to fix sign not returning to original text - obsolete
	/*
	internal static void RestoreSignImmediately(Vector3i pos)
	{
		World world = GameManager.Instance.World;
		TryRestoreOriginalMessage(world, pos, broadcastToPlayers: true);
	}
	*/
}
