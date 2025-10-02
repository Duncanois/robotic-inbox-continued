using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RoboticInbox.Utilities;

internal class StorageManager
{
    private static readonly ModLog<StorageManager> _log = new ModLog<StorageManager>();

    public const int Y_MIN = 0;
    public const int Y_MAX = 253;

    // Use TagGroup.Global for FastTags<T>
    private static FastTags<TagGroup.Global> RoboticInboxTags { get; } = FastTags<TagGroup.Global>.Parse("roboticinbox,roboticinboxinsecure");
    private static FastTags<TagGroup.Global> RoboticSecureInboxTag { get; } = FastTags<TagGroup.Global>.Parse("roboticinbox");
    private static FastTags<TagGroup.Global> RoboticInsecureInboxTag { get; } = FastTags<TagGroup.Global>.Parse("roboticinboxinsecure");
    private static FastTags<TagGroup.Global> RepairableLockTag { get; } = FastTags<TagGroup.Global>.Parse("repairablelock");

    private static int LandClaimRadius { get; set; }

    public static Dictionary<Vector3i, Coroutine> ActiveCoroutines { get; private set; } = new Dictionary<Vector3i, Coroutine>();

    internal static void OnGameStartDone()
    {
        if (!SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer)
        {
            _log.Warn("Mod recognizes you as a client, so this locally installed mod will be inactive until you host a game.");
            return;
        }
        _log.Info("Mod recognizes you as the host, so it will begin managing containers.");
        _log.Info("Attempting to verify blocks for Robotic Inbox mod.");
        foreach (KeyValuePair<string, Block> item in Block.nameToBlock)
        {
            if (item.Value.Tags.Test_AnySet(RoboticSecureInboxTag))
            {
                _log.Info($"{item.Value.blockName} (block id: {item.Value.blockID}) verified as a Robotic Inbox Block.");
            }
            if (HasRoboticInboxInsecureTag(item.Value))
            {
                _log.Info($"{item.Value.blockName} (block id: {item.Value.blockID}) verified as an Insecure Robotic Inbox Block.");
            }
            if (HasRepairableLockTag(item.Value))
            {
                _log.Info($"{item.Value.blockName} (block id: {item.Value.blockID}) verified as a Block with a Repairable Lock.");
            }
        }
        int num = GameStats.GetInt((EnumGameStats)44);
        LandClaimRadius = ((num % 2 == 1) ? (num - 1) : num) / 2;
        _log.Info($"LandClaimRadius found to be {LandClaimRadius}m");
    }

    internal static bool HasRoboticInboxSecureTag(Block block)
    {
        return block.Tags.Test_AnySet(RoboticSecureInboxTag);
    }

    internal static bool HasRoboticInboxInsecureTag(Block block)
    {
        return block.Tags.Test_AnySet(RoboticInsecureInboxTag);
    }

    internal static bool HasRepairableLockTag(Block block)
    {
        return block.Tags.Test_AnySet(RepairableLockTag);
    }

    internal static void OnGameManagerApplicationQuit()
    {
        if (ActiveCoroutines.Count > 0)
        {
            _log.Trace($"Stopping {ActiveCoroutines.Count} active coroutines for shutdown.");
            foreach (KeyValuePair<Vector3i, Coroutine> activeCoroutine in ActiveCoroutines)
            {
                ThreadManager.StopCoroutine(activeCoroutine.Value);
            }
            _log.Trace("All scanning coroutines stopped for shutdown.");
        }
        else
        {
            _log.Trace("No scanning coroutines needed to be stopped for shutdown.");
        }
    }

    internal static void Distribute(int clrIdx, Vector3i sourcePos)
    {
        _log.Trace($"Distribute called for tile entity at {sourcePos}");
        World world = GameManager.Instance.World;
        TileEntity tileEntity = ((WorldBase)world).GetTileEntity(clrIdx, sourcePos);
        if (tileEntity != null)
        {
            BlockValue blockValue = tileEntity.blockValue;
            if (blockValue.Block != null)
            {
                blockValue = tileEntity.blockValue;
                if (!HasRoboticInboxSecureTag(blockValue.Block))
                {
                    _log.Trace($"!InboxBlockIds.Contains(source.blockValue.Block.blockID) at {sourcePos} -- InboxBlockIds does not contain {blockValue.Block.blockID}");
                    return;
                }
                _log.Trace("TileEntity block id confirmed as a Robotic Inbox Block");
                if (!TryCastAsContainer(tileEntity, out var typed))
                {
                    _log.Trace($"TileEntity at {sourcePos} could not be converted into a TileEntityLootContainer.");
                    return;
                }
                Vector3i min, max;
                GetBoundsWithinWorldAndLandClaim(sourcePos, out min, out max);
                if (min == max)
                {
                    _log.Trace("Min and Max ranges to scan for containers are equal, so there is no range to scan containers within.");
                }
                else
                {
                    ActiveCoroutines.Add(sourcePos, ThreadManager.StartCoroutine(OrganizeCoroutine(world, clrIdx, sourcePos, tileEntity, typed, min, max)));
                }
                return;
            }
        }
        _log.Trace($"TileEntity not found at {sourcePos}");
    }

    private static void GetBoundsWithinWorldAndLandClaim(Vector3i source, out Vector3i min, out Vector3i max)
    {
        min = (max = default(Vector3i));
        Vector3i val = default(Vector3i);
        Vector3i val2 = default(Vector3i);
        if (!GameManager.Instance.World.GetWorldExtent(out val, out val2))
        {
            _log.Warn("World.GetWorldExtent failed when checking for limits; this is not expected and may indicate an error.");
            return;
        }
        _log.Trace($"minMapSize: {val}, maxMapSize: {val2}, actualMapSize: {val2 - val}");
        if (SettingsManager.BaseSiphoningProtection && TryGetActiveLandClaimPosContaining(source, out var lcbPos))
        {
            _log.Trace($"Land Claim was found containing {source} (pos: {lcbPos}); clamping to world and land claim coordinates.");
            min.x = FastMax(source.x - SettingsManager.InboxHorizontalRange, lcbPos.x - LandClaimRadius, val.x);
            max.x = FastMin(source.x + SettingsManager.InboxHorizontalRange, lcbPos.x + LandClaimRadius, val2.x);
            min.z = FastMax(source.z - SettingsManager.InboxHorizontalRange, lcbPos.z - LandClaimRadius, val.z);
            max.z = FastMin(source.z + SettingsManager.InboxHorizontalRange, lcbPos.z + LandClaimRadius, val2.z);
            if (SettingsManager.InboxVerticalRange == -1)
            {
                min.y = Utils.FastMax(0, val.y);
                max.y = Utils.FastMin(253, val2.y);
            }
            else
            {
                min.y = FastMax(source.y - SettingsManager.InboxVerticalRange, 0, val.y);
                max.y = FastMin(source.y + SettingsManager.InboxVerticalRange, 253, val2.y);
            }
            _log.Trace($"clampedMin: {min}, clampedMax: {max}.");
        }
        else
        {
            _log.Trace($"Land Claim not found containing {source}; clamping to world coordinates only.");
            min.x = Utils.FastMax(source.x - SettingsManager.InboxHorizontalRange, val.x);
            max.x = Utils.FastMin(source.x + SettingsManager.InboxHorizontalRange, val2.x);
            min.z = Utils.FastMax(source.z - SettingsManager.InboxHorizontalRange, val.z);
            max.z = Utils.FastMin(source.z + SettingsManager.InboxHorizontalRange, val2.z);
            if (SettingsManager.InboxVerticalRange == -1)
            {
                min.y = Utils.FastMax(0, val.y);
                max.y = Utils.FastMin(253, val2.y);
            }
            else
            {
                min.y = FastMax(source.y - SettingsManager.InboxVerticalRange, 0, val.y);
                max.y = FastMin(source.y + SettingsManager.InboxVerticalRange, 253, val2.y);
            }
            _log.Trace($"clampedMin: {min}, clampedMax: {max}.");
        }
    }

    private static bool TryGetActiveLandClaimPosContaining(Vector3i sourcePos, out Vector3i lcbPos)
    {
        World world = GameManager.Instance.World;
        foreach (KeyValuePair<PlatformUserIdentifierAbs, PersistentPlayerData> player in GameManager.Instance.persistentPlayers.Players)
        {
            if (!world.IsLandProtectionValidForPlayer(player.Value))
            {
                continue;
            }
            foreach (Vector3i landProtectionBlock in player.Value.GetLandProtectionBlocks())
            {
                if (sourcePos.x >= landProtectionBlock.x - LandClaimRadius && sourcePos.x <= landProtectionBlock.x + LandClaimRadius && sourcePos.z >= landProtectionBlock.z - LandClaimRadius && sourcePos.z <= landProtectionBlock.z + LandClaimRadius)
                {
                    lcbPos = landProtectionBlock;
                    return true;
                }
            }
        }
        lcbPos = default(Vector3i);
        return false;
    }

    private static int FastMax(int v1, int v2, int v3)
    {
        return Utils.FastMax(v1, Utils.FastMax(v2, v3));
    }

    private static int FastMin(int v1, int v2, int v3)
    {
        return Utils.FastMin(v1, Utils.FastMin(v2, v3));
    }

    private static int FindMaxDistance(Vector3i v1, Vector3i v2)
    {
        int num = 0;
        foreach (int item in new List<int> { v1.z, v1.y, v1.z, v2.x, v2.y, v2.z })
        {
            if (num < item)
            {
                num = item;
            }
        }
        return num;
    }

    private static bool IsWithin(int x, int y, int z, Vector3i min, Vector3i max)
    {
        if (x >= min.x && x <= max.x && y >= min.y && y <= max.y && z >= min.z)
        {
            return z <= max.z;
        }
        return false;
    }

    private static IEnumerator OrganizeCoroutine(World world, int clrIdx, Vector3i sourcePos, TileEntity source, ITileEntityLootable sourceContainer, Vector3i min, Vector3i max)
    {
        MarkInUse((ITileEntity)(object)source, -1);
        _log.Trace($"[{sourcePos}] starting organize coroutine");
        int maxDist = FindMaxDistance(sourcePos - min, max - sourcePos);
        for (int distance = 1; distance <= maxDist; distance++)
        {
            Vector3i targetPos;
            TileEntity target;
            ITileEntityLootable targetContainer;
            if (sourcePos.y - distance >= min.y)
            {
                int y = sourcePos.y - distance;
                for (int x = Utils.FastMax(sourcePos.x - distance, min.x); x <= Utils.FastMin(sourcePos.x + distance, max.x); x++)
                {
                    for (int z = Utils.FastMax(sourcePos.z - distance, min.z); z <= Utils.FastMax(sourcePos.z + distance, max.z); z++)
                    {
                        if (VerifyContainer(world, clrIdx, x, y, z, out targetPos, out target, out targetContainer))
                        {
                            yield return null;
                            Distribute(source, sourceContainer, sourcePos, target, targetContainer, targetPos);
                        }
                        target = null;
                        targetContainer = null;
                    }
                }
            }
            if (sourcePos.y + distance <= max.y)
            {
                int y = sourcePos.y + distance;
                for (int x = Utils.FastMax(sourcePos.x - distance, min.x); x <= Utils.FastMin(sourcePos.x + distance, max.x); x++)
                {
                    for (int z = Utils.FastMax(sourcePos.z - distance, min.z); z <= Utils.FastMax(sourcePos.z + distance, max.z); z++)
                    {
                        if (VerifyContainer(world, clrIdx, x, y, z, out targetPos, out target, out targetContainer))
                        {
                            yield return null;
                            Distribute(source, sourceContainer, sourcePos, target, targetContainer, targetPos);
                        }
                        target = null;
                        targetContainer = null;
                    }
                }
            }
            if (sourcePos.z - distance >= min.z)
            {
                int y = sourcePos.z - distance;
                for (int x = Utils.FastMax(sourcePos.y - distance + 1, min.y); x <= Utils.FastMin(sourcePos.y + distance - 1, max.y); x++)
                {
                    for (int z = Utils.FastMax(sourcePos.x - distance, min.x); z <= Utils.FastMin(sourcePos.x + distance, max.x); z++)
                    {
                        if (VerifyContainer(world, clrIdx, z, x, y, out targetPos, out target, out targetContainer))
                        {
                            yield return null;
                            Distribute(source, sourceContainer, sourcePos, target, targetContainer, targetPos);
                        }
                        target = null;
                        targetContainer = null;
                    }
                }
            }
            if (sourcePos.z + distance <= max.z)
            {
                int y = sourcePos.z + distance;
                for (int x = Utils.FastMax(sourcePos.y - distance + 1, min.y); x <= Utils.FastMin(sourcePos.y + distance - 1, max.y); x++)
                {
                    for (int z = Utils.FastMax(sourcePos.x - distance, min.x); z <= Utils.FastMin(sourcePos.x + distance, max.x); z++)
                    {
                        if (VerifyContainer(world, clrIdx, z, x, y, out targetPos, out target, out targetContainer))
                        {
                            yield return null;
                            Distribute(source, sourceContainer, sourcePos, target, targetContainer, targetPos);
                        }
                        target = null;
                        targetContainer = null;
                    }
                }
            }
            if (sourcePos.x - distance >= min.x)
            {
                int y = sourcePos.x - distance;
                for (int x = Utils.FastMax(sourcePos.y - distance + 1, min.y); x <= Utils.FastMin(sourcePos.y + distance - 1, max.y); x++)
                {
                    for (int z = Utils.FastMax(sourcePos.z - distance + 1, min.z); z <= Utils.FastMin(sourcePos.z + distance - 1, max.z); z++)
                    {
                        if (VerifyContainer(world, clrIdx, y, x, z, out targetPos, out target, out targetContainer))
                        {
                            yield return null;
                            Distribute(source, sourceContainer, sourcePos, target, targetContainer, targetPos);
                        }
                        target = null;
                        targetContainer = null;
                    }
                }
            }
            if (sourcePos.x + distance <= max.x)
            {
                int y = sourcePos.x + distance;
                for (int x = Utils.FastMax(sourcePos.y - distance + 1, min.y); x <= Utils.FastMin(sourcePos.y + distance - 1, max.y); x++)
                {
                    for (int z = Utils.FastMax(sourcePos.z - distance + 1, min.z); z <= Utils.FastMin(sourcePos.z + distance - 1, max.z); z++)
                    {
                        if (VerifyContainer(world, clrIdx, y, x, z, out targetPos, out target, out targetContainer))
                        {
                            yield return null;
                            Distribute(source, sourceContainer, sourcePos, target, targetContainer, targetPos);
                        }
                        target = null;
                        targetContainer = null;
                    }
                }
            }
            yield return null;
        }
        _log.Trace($"[{sourcePos}] ending organize coroutine");
        ActiveCoroutines.Remove(sourcePos);
        MarkNotInUse((ITileEntity)(object)source);
    }

    private static bool VerifyContainer(World world, int clrIdx, int x, int y, int z, out Vector3i pos, out TileEntity tileEntity, out ITileEntityLootable tileEntityLootContainer)
    {
        pos = new Vector3i(x, y, z);
        tileEntity = ((WorldBase)world).GetTileEntity(clrIdx, pos);
        if (TryCastAsContainer(tileEntity, out tileEntityLootContainer) && tileEntityLootContainer.bPlayerStorage && !tileEntityLootContainer.bPlayerBackpack)
        {
            BlockValue blockValue = tileEntity.blockValue;
            return !IsRoboticInbox(blockValue.Block);
        }
        return false;
    }

    private static bool IsRoboticInbox(Block block)
    {
        return block.Tags.Test_AnySet(RoboticInboxTags);
    }

    private static bool CheckAndHandleInUse(TileEntity source, Vector3i sourcePos, TileEntity target, Vector3i targetPos)
    {
        int entityIDForLockedTileEntity = GameManager.Instance.GetEntityIDForLockedTileEntity(source);
        if (entityIDForLockedTileEntity != -1)
        {
            _log.Trace($"player {entityIDForLockedTileEntity} is currently accessing source container at {sourcePos}; skipping");
            NotificationManager.PlaySoundVehicleStorageOpen((Vector3)sourcePos);
            return true;
        }
        int entityIDForLockedTileEntity2 = GameManager.Instance.GetEntityIDForLockedTileEntity(target);
        if (entityIDForLockedTileEntity2 != -1)
        {
            _log.Trace($"player {entityIDForLockedTileEntity2} is currently accessing target container at {targetPos}; skipping");
            NotificationManager.NotifyInUse(entityIDForLockedTileEntity2, (Vector3)targetPos);
            return true;
        }
        return false;
    }

    private static void Distribute(TileEntity source, ITileEntityLootable sourceContainer, Vector3i sourcePos, TileEntity target, ITileEntityLootable targetContainer, Vector3i targetPos)
    {
        if (CheckAndHandleInUse(source, sourcePos, target, targetPos))
        {
            _log.Trace("returning early");
            return;
        }
        if (!CanAccess(source, target, targetPos))
        {
            NotificationManager.PlaySoundVehicleStorageOpen((Vector3)targetPos);
            return;
        }
        try
        {
            int num = 0;
            for (int i = 0; i < sourceContainer.items.Length; i++)
            {
                if (((object)ItemStack.Empty).Equals((object)sourceContainer.items[i]))
                {
                    continue;
                }
                bool flag = false;
                bool flag2 = false;
                int count = sourceContainer.items[i].count;
                for (int j = 0; j < targetContainer.items.Length; j++)
                {
                    if (targetContainer.items[j].itemValue.ItemClass == sourceContainer.items[i].itemValue.ItemClass)
                    {
                        flag = true;
                        var stackResult = ((IInventory)targetContainer).TryStackItem(j, sourceContainer.items[i]);
                        if (stackResult.Item2)
                        {
                            // Remove the items that were stacked from the source slot
                            sourceContainer.UpdateSlot(i, ItemStack.Empty);
                            num += count;
                            flag2 = true;
                            break;
                        }
                    }
                }
                if (flag && !flag2)
                {
                    if (((IInventory)targetContainer).AddItem(sourceContainer.items[i]))
                    {
                        sourceContainer.UpdateSlot(i, ItemStack.Empty);
                        num += count;
                    }
                    else
                    {
                        num += count - sourceContainer.items[i].count;
                    }
                }
            }
            if (num > 0)
            {
                targetContainer.items = StackSortUtil.CombineAndSortStacks(targetContainer.items, 0, null);
                SignManager.HandleTransferred(targetPos, target, num);
            }
            // Always refresh the source container after distribution
            sourceContainer.SetModified();
        }
        catch (Exception e)
        {
            _log.Error("encountered issues organizing with Inbox", e);
        }
    }

    private static bool CanAccess(TileEntity source, TileEntity target, Vector3i targetPos)
    {
        ILockable typed;
        bool flag = TryCastAsLock(source, out typed);
        if (!TryCastAsLock(target, out var typed2))
        {
            return true;
        }
        if (!typed2.IsLocked())
        {
            return true;
        }
        if (!typed2.HasPassword())
        {
            SignManager.HandleTargetLockedWithoutPassword(targetPos, target);
            return false;
        }
        if (!flag || !typed.IsLocked())
        {
            SignManager.HandleTargetLockedWhileSourceIsNot(targetPos, target);
            return false;
        }
        if (typed.GetPassword().Equals(typed2.GetPassword()))
        {
            return true;
        }
        SignManager.HandlePasswordMismatch(targetPos, target);
        return false;
    }

    private static bool TryCastAsContainer(TileEntity entity, out ITileEntityLootable typed)
    {
        if (entity != null)
        {
            if (IsCompositeStorage(entity))
            {
                typed = (ITileEntityLootable)(object)((TileEntityComposite)((entity is TileEntityComposite) ? entity : null)).GetFeature<TEFeatureStorage>();
                return typed != null;
            }
            if (IsNonCompositeStorage(entity))
            {
                typed = (ITileEntityLootable)(object)((entity is ITileEntityLootable) ? entity : null);
                return typed != null;
            }
        }
        typed = null;
        return false;
    }

    private static bool IsCompositeStorage(TileEntity entity)
    {
        if ((int)entity.GetTileEntityType() == 25)
        {
            return ((TileEntityComposite)((entity is TileEntityComposite) ? entity : null)).GetFeature<TEFeatureStorage>() != null;
        }
        return false;
    }

    private static bool IsNonCompositeStorage(TileEntity entity)
    {
        if ((int)entity.GetTileEntityType() != 5 && (int)entity.GetTileEntityType() != 10)
        {
            return (int)entity.GetTileEntityType() == 22;
        }
        return true;
    }

    private static bool TryCastAsLock(TileEntity entity, out ILockable typed)
    {
        if (IsCompositeLock(entity))
        {
            typed = (ILockable)(object)((TileEntityComposite)((entity is TileEntityComposite) ? entity : null)).GetFeature<TEFeatureLockable>();
            return typed != null;
        }
        if (IsLock(entity))
        {
            typed = (ILockable)(object)((entity is ILockable) ? entity : null);
            return typed != null;
        }
        typed = null;
        return false;
    }

    private static bool IsCompositeLock(TileEntity entity)
    {
        if (entity != null && entity is TileEntityComposite)
        {
            return ((TileEntityComposite)((entity is TileEntityComposite) ? entity : null)).GetFeature<TEFeatureLockable>() != null;
        }
        return false;
    }

    private static bool IsLock(TileEntity entity)
    {
        if ((int)entity.GetTileEntityType() != 10)
        {
            return (int)entity.GetTileEntityType() == 22;
        }
        return true;
    }

    private static void MarkInUse(ITileEntity tileEntity, int entityIdThatOpenedIt)
    {
        if (!GameManager.Instance.lockedTileEntities.ContainsKey(tileEntity))
        {
            _log.Trace("MarkInUse: marked tile entity confirmed as being in-use");
            GameManager.Instance.lockedTileEntities.Add(tileEntity, entityIdThatOpenedIt);
        }
        else
        {
            _log.Trace("MarkInUse: tile entity was already marked as being in-use");
        }
    }

    private static void MarkNotInUse(ITileEntity tileEntity)
    {
        if (GameManager.Instance.lockedTileEntities.Remove(tileEntity))
        {
            _log.Trace("MarkNotInUse: marked tileEntity as no longer being in use");
        }
        else
        {
            _log.Trace("MarkNotInUse: tileEntity was not present in lockedTileEntities list");
        }
    }

    internal static void ForceContainerRefresh(int clrIdx, Vector3i blockPos)
    {
        World world = GameManager.Instance.World;
        TileEntity tileEntity = ((WorldBase)world).GetTileEntity(clrIdx, blockPos);
        if (tileEntity != null && TryCastAsContainer(tileEntity, out var container))
        {
            container.SetModified();
        }
    }
}
