using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using RedLoader;
using RedLoader.Preferences;
using Sons.Inventory;
using Sons.Items.Core;
using SonsSdk;
using SUI;
using TheForest.Utils;
using ItemInstanceAndCount = Sons.Inventory.ItemInstance.ItemInstanceAndCount;
using ItemTypes = Sons.Items.Core.Types;

namespace PickupMultiplier;

public class PickupMultiplierMod : SonsMod
{
    private const string ModName = "PickupMultiplier";
    private const string ModVersion = "2.1.1";
    private const int DefaultMultiplier = 100;
    private const int MaxMultiplier = 100;
    private const int ActivityTailLength = 20;

    public const string ModId = ModName;

    private readonly PickupMultiplierSettings _settings = new();
    private readonly HashSet<int> _excludedItemIds = new();
    private readonly Dictionary<int, ConfigEntry<int>> _itemMultipliers = new();
    private readonly List<string> _activity = new();
    private string _statusPath;

    protected override void OnEarlyInitializeMelon()
    {
        ConfigureSettings();
    }

    protected override void OnInitializeMod()
    {
        _statusPath = ConfigSystem.GetFilePath($"{ModName}.status.txt");
        ReloadExcludedItemIds();
        WriteStatusFile();
        Log($"[{ModName}] x{_settings.Multiplier.Value} enabled={_settings.Enabled.Value} excluded={_excludedItemIds.Count}");
    }

    protected override void OnSdkInitialized()
    {
        ApplyPatches();
        SdkEvents.OnItemPickup.Subscribe(OnItemPickup);
        SettingsRegistry.CreateSettings(this, _settings, false, OnSettingsClosed);
    }

    private void ApplyPatches()
    {
        TryPatch(typeof(SettingsPanelOpenPatch));
    }

    private void TryPatch(Type patchType)
    {
        if (HarmonyInstance == null)
        {
            Log($"[{ModName}] no harmony instance, skipping {patchType.Name}");
            return;
        }

        try
        {
            HarmonyInstance.CreateClassProcessor(patchType).Patch();
        }
        catch (Exception error)
        {
            Log($"[{ModName}] {patchType.Name} not applied: {error.Message}");
        }
    }

    private void ConfigureSettings()
    {
        ConfigSystem.CreateFileCategory(ModName, "Pickup Multiplier", PickupMultiplierSettings.FileName);

        _settings.Enabled = ConfigSystem.CreateEntry(ModName, "enabled", true, "Enabled",
            "Multiply the items you pick up", false, false, null);

        var range = new ValueRange<int>(1, MaxMultiplier);
        _settings.Multiplier = ConfigSystem.CreateEntry(ModName, "multiplier", DefaultMultiplier, "Multiplier",
            "Multiply regular pickups by this much; item sliders follow it until you change them", false, false, range);
        _settings.Multiplier.SetRange(1, MaxMultiplier);

        _settings.ExcludedItemIds = ConfigSystem.CreateEntry(ModName, "excludedItemIds", string.Empty, "Excluded item ids",
            "Comma separated item ids that are never multiplied", false, false, null);

        _settings.RegisterItemEntries(_itemMultipliers, _settings.Multiplier.Value);
        _settings.ExcludedItemIds.OnValueChanged.Subscribe((_, _) => ReloadExcludedItemIds());
    }

    private void OnItemPickup(ItemInstanceAndCount picked)
    {
        var itemInstance = picked?.ItemInstance;
        if (itemInstance == null)
        {
            return;
        }

        var inventory = LocalPlayer.Inventory;
        if (inventory == null)
        {
            return;
        }

        var itemInstanceManager = inventory._itemInstanceManager;
        if (itemInstanceManager == null)
        {
            return;
        }

        var grantedAmount = ResolveGrant(itemInstance, picked.Count, itemInstanceManager);
        if (grantedAmount <= 0)
        {
            return;
        }

        // adding to the inventory does not raise the pickup game event, so this cannot recurse
        itemInstanceManager.TryAddItems(itemInstance._itemID, grantedAmount, false);
        RecordActivity(itemInstance, picked.Count, grantedAmount);
    }

    private int ResolveGrant(ItemInstance itemInstance, int pickedCount, ItemInstanceManager itemInstanceManager)
    {
        if (pickedCount <= 0 || !_settings.Enabled.Value)
        {
            return 0;
        }

        var itemId = itemInstance._itemID;
        var multiplier = ResolveMultiplier(itemId, out _);
        if (multiplier <= 1)
        {
            return 0;
        }

        var itemData = itemInstance.Data;
        if (!IsRegularPickup(itemData, itemId))
        {
            return 0;
        }

        var availableRoom = ResolveAvailableRoom(itemInstanceManager, itemData, itemId);
        if (availableRoom <= 0)
        {
            return 0;
        }

        return Math.Min(pickedCount * (multiplier - 1), availableRoom);
    }

    private int ResolveMultiplier(int itemId, out bool isItemOverride)
    {
        if (_itemMultipliers.TryGetValue(itemId, out var entry) && entry != null && entry.Value != entry.DefaultValue)
        {
            isItemOverride = true;
            return entry.Value;
        }

        isItemOverride = false;
        return _settings.Multiplier.Value;
    }

    private void SyncItemDefaults(int globalMultiplier)
    {
        foreach (var entry in _itemMultipliers.Values)
        {
            if (entry == null || entry.Value != entry.DefaultValue)
            {
                continue;
            }

            entry.DefaultValue = globalMultiplier;
            entry.Value = globalMultiplier;
        }
    }

    // pushing a block past its max count corrupts the native inventory, so a grant never exceeds the free space
    private int ResolveAvailableRoom(ItemInstanceManager itemInstanceManager, ItemData itemData, int itemId)
    {
        var maxCount = ResolveMaxCount(itemInstanceManager, itemId);
        if (maxCount <= 0)
        {
            maxCount = itemData._maxAmount;
        }

        return maxCount - itemInstanceManager.GetItemCount(itemId);
    }

    private int ResolveMaxCount(ItemInstanceManager itemInstanceManager, int itemId)
    {
        var itemsMap = itemInstanceManager._itemsMap;
        if (itemsMap == null)
        {
            return 0;
        }

        if (!itemsMap.TryGetValue(itemId, out var block) || block == null)
        {
            return 0;
        }

        return block.MaxCount;
    }

    private bool IsRegularPickup(ItemData itemData, int itemId)
    {
        if (itemData == null || _excludedItemIds.Contains(itemId))
        {
            return false;
        }

        if (itemData._eachInstanceIsUnique || itemData._maxAmount <= 1)
        {
            return false;
        }

        if (itemData._type.HasFlag(ItemTypes.UniqueItem))
        {
            return false;
        }

        if (itemData._type.HasFlag(ItemTypes.Special))
        {
            return false;
        }

        return !itemData._type.HasFlag(ItemTypes.StructureBlueprint);
    }

    private void ReloadExcludedItemIds()
    {
        _excludedItemIds.Clear();

        var text = _settings.ExcludedItemIds == null ? null : _settings.ExcludedItemIds.Value;
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        foreach (var part in text.Split(','))
        {
            if (int.TryParse(part.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var itemId))
            {
                _excludedItemIds.Add(itemId);
            }
        }
    }

    private void RecordActivity(ItemInstance itemInstance, int pickedCount, int grantedAmount)
    {
        var itemData = itemInstance.Data;
        var name = itemData == null ? "?" : itemData._name;
        var multiplier = ResolveMultiplier(itemInstance._itemID, out var isItemOverride);
        var source = isItemOverride ? "item slider" : "global";
        _activity.Add($"{itemInstance._itemID} {name} picked {pickedCount} -> granted {grantedAmount} (x{multiplier}, {source})");

        while (_activity.Count > ActivityTailLength)
        {
            _activity.RemoveAt(0);
        }

        WriteStatusFile();
    }

    private void OnSettingsClosed()
    {
        ReloadExcludedItemIds();
        SyncItemDefaults(_settings.Multiplier.Value);
        WriteStatusFile();
        Log($"[{ModName}] settings applied: x{_settings.Multiplier.Value} enabled={_settings.Enabled.Value} excluded={_excludedItemIds.Count}");
    }

    private void WriteStatusFile()
    {
        if (string.IsNullOrEmpty(_statusPath))
        {
            return;
        }

        var lines = new List<string>
        {
            $"mod         {ModName} {ModVersion}",
            $"loaded      {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)}",
            $"enabled     {_settings.Enabled.Value}",
            $"multiplier  {_settings.Multiplier.Value}",
            $"excluded    {_excludedItemIds.Count}",
            "recent pickups:"
        };

        lines.AddRange(_activity.Select(entry => $"  {entry}"));
        File.WriteAllLines(_statusPath, lines);
    }
}
