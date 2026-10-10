using HarmonyLib;
using SettingsEntry = SUI.SettingsRegistry.SettingsEntry;

namespace PickupMultiplier;

[HarmonyPatch(typeof(SettingsEntry), nameof(SettingsEntry.ToggleCategory))]
internal static class SettingsCollapsePatch
{
    private static void Postfix(string identifier, bool show)
    {
        ItemSearchBar.OnCategoryToggled(identifier, show);
    }
}

[HarmonyPatch(typeof(SonsGameManager.ModSettingsUi), nameof(SonsGameManager.ModSettingsUi.Open))]
internal static class SettingsPanelOpenPatch
{
    private static void Postfix(string id)
    {
        if (id != PickupMultiplierMod.ModId)
        {
            return;
        }

        var settingsEntry = SUI.SettingsRegistry.GetEntry(id);
        if (settingsEntry == null)
        {
            return;
        }

        ItemSearchBar.OnSettingsPanelOpened(settingsEntry);
    }
}
