using System.Collections.Generic;
using RedLoader;
using RedLoader.Preferences;
using SonsSdk.Attributes;

namespace PickupMultiplier;

[SettingsUiMode(SettingsUiMode.ESettingsUiMode.OptIn)]
public partial class PickupMultiplierSettings
{
    public const string FileName = "PickupMultiplier.cfg";
    public const int MaxItemMultiplier = 25;

    private static readonly ValueRange<int> ItemMultiplierRange = new(0, MaxItemMultiplier);

    [SettingsUiInclude]
    [SettingsUiHeader("Pickup multiplier")]
    public ConfigEntry<bool> Enabled;

    [SettingsUiInclude]
    public ConfigEntry<int> Multiplier;

    [SettingsUiInclude]
    public ConfigEntry<string> ExcludedItemIds;

    private ConfigEntry<int> RegisterItem(ConfigCategory category, Dictionary<int, ConfigEntry<int>> itemEntries, int itemId,
        string key, string label, string description, int globalMultiplier)
    {
        var entry = category.CreateEntry(key, globalMultiplier, label, description, false, true, ItemMultiplierRange);
        entry.SetRange(0, MaxItemMultiplier);
        itemEntries[itemId] = entry;
        return entry;
    }
}
