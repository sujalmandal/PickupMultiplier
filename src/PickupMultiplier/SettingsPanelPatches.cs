using HarmonyLib;

namespace PickupMultiplier;

[HarmonyPatch(typeof(SonsGameManager.ModSettingsUi), nameof(SonsGameManager.ModSettingsUi.Open))]
internal static class SettingsPanelOpenPatch
{
    private static void Postfix(string id)
    {
        if (id != PickupMultiplierMod.ModId)
        {
            return;
        }

        SettingsPagePolish.Apply(SUI.SettingsRegistry.GetEntry(id));
    }
}
