using UnityEngine;

namespace RoboticInbox.Utilities;

internal class NotificationManager
{
	private static string SoundVehicleStorageOpen { get; } = "vehicle_storage_open";

	private static string SoundVehicleStorageClose { get; } = "vehicle_storage_close";

	private static string MessageTargetContainerInUse { get; } = "Robotic Inbox was [ff8000]unable to organize this container[-] as it was in use.";

	public static void PlaySoundVehicleStorageOpen(Vector3 pos)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		GameManager.Instance.PlaySoundAtPositionServer(pos, SoundVehicleStorageOpen, (AudioRolloffMode)0, 5);
	}

	public static void PlaySoundVehicleStorageClose(Vector3 pos)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		GameManager.Instance.PlaySoundAtPositionServer(pos, SoundVehicleStorageClose, (AudioRolloffMode)0, 5);
	}

	internal static void NotifyInUse(int entityIdInTargetContainer, Vector3 targetPos)
	{
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		ClientInfo val = SingletonMonoBehaviour<ConnectionManager>.Instance.Clients.ForEntityId(entityIdInTargetContainer);
		if (val == null)
		{
			GameManager.ShowTooltip(((WorldBase)GameManager.Instance.World).GetPrimaryPlayer(), MessageTargetContainerInUse, false, false, 0f);
		}
		else
		{
			val.SendPackage((NetPackage)(object)NetPackageManager.GetPackage<NetPackageShowToolbeltMessage>().Setup(MessageTargetContainerInUse, SoundVehicleStorageOpen));
		}
		GameManager.Instance.PlaySoundAtPositionServer(targetPos, SoundVehicleStorageOpen, (AudioRolloffMode)0, 5);
	}
}
