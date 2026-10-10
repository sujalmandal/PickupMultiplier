using System.Collections.Generic;
using RedLoader;
using SUI;
using TMPro;
using UnityEngine;
using SettingsEntry = SUI.SettingsRegistry.SettingsEntry;

using static SUI.SUI;

namespace PickupMultiplier;

public static class SettingsPagePolish
{
    private const string PanelId = "ModSettingsPanel";
    private const string PlaceholderMarker = "IPSUM";
    private const string ExpandedGlyph = "\uf0d7";
    private const string CollapsedGlyph = "\uf0da";

    public static void Apply(SettingsEntry settingsEntry)
    {
        if (settingsEntry == null)
        {
            return;
        }

        var panel = GetPanel(PanelId);
        if (panel == null)
        {
            return;
        }

        var scroll = FindScrollRoot(panel);
        RepairDividers(settingsEntry, scroll);
        RepairTitle(panel);
        PolishSliders(settingsEntry);
    }

    private static RectTransform FindScrollRoot(SPanelOptions panel)
    {
        var scrollRect = panel.Root.GetComponentInChildren<UnityEngine.UI.ScrollRect>(true);
        if (scrollRect == null)
        {
            return null;
        }

        var target = scrollRect.transform;
        while (target.parent != null && target.parent != panel.Root.transform)
        {
            target = target.parent;
        }

        return target as RectTransform;
    }

    // SUI reuses the game's option prefabs, and a reused label can still carry the game's placeholder
    // text, so any divider that does not name its own category gets the text it should have had
    private static void RepairDividers(SettingsEntry settingsEntry, RectTransform scroll)
    {
        if (scroll == null)
        {
            return;
        }

        var categories = ResolveCategoryOrder(settingsEntry);
        var dividers = new List<Transform>();
        foreach (var candidate in scroll.GetComponentsInChildren<Transform>(true))
        {
            if (candidate.name == "Divider")
            {
                dividers.Add(candidate);
            }
        }

        for (var i = 0; i < dividers.Count; i++)
        {
            var label = dividers[i].GetComponentInChildren<TextMeshProUGUI>(true);
            if (label == null)
            {
                continue;
            }

            var category = i < categories.Count ? categories[i] : "Items";
            if (Matches(label.text, category))
            {
                continue;
            }

            var glyph = label.text != null && label.text.StartsWith(CollapsedGlyph) ? CollapsedGlyph : ExpandedGlyph;
            label.text = $"{glyph} {category}";
            RLog.Msg($"[{PickupMultiplierMod.ModId}] repaired divider label to '{glyph} {category}'");
        }
    }

    private static void RepairTitle(SPanelOptions panel)
    {
        foreach (Transform child in panel.Root.transform)
        {
            var label = child.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label == null || !IsPlaceholder(label.text))
            {
                continue;
            }

            label.text = "PICKUP MULTIPLIER";
            RLog.Msg($"[{PickupMultiplierMod.ModId}] repaired the settings panel title");
        }
    }

    private static void PolishSliders(SettingsEntry settingsEntry)
    {
        foreach (var entry in settingsEntry.ConfigEntries)
        {
            if (entry == null || entry.ConfigEntry == null)
            {
                continue;
            }

            var slider = settingsEntry.GetWrappedElementForEntry(entry.ConfigEntry)?.As<SSliderOptions>();
            if (slider == null)
            {
                continue;
            }

            slider.IntStep().Format("0");
            if (entry.ConfigEntry.BoxedValue is int value)
            {
                slider.Value(value);
            }
        }
    }

    private static List<string> ResolveCategoryOrder(SettingsEntry settingsEntry)
    {
        var order = new List<string>();
        foreach (var entry in settingsEntry.ConfigEntries)
        {
            if (entry == null || string.IsNullOrEmpty(entry.CategoryName))
            {
                continue;
            }

            if (!order.Contains(entry.CategoryName))
            {
                order.Add(entry.CategoryName);
            }
        }

        return order;
    }

    private static bool Matches(string text, string category)
    {
        return !string.IsNullOrEmpty(text) && text.ToLowerInvariant().Contains(category.ToLowerInvariant());
    }

    private static bool IsPlaceholder(string text)
    {
        return !string.IsNullOrEmpty(text) && text.ToUpperInvariant().Contains(PlaceholderMarker);
    }
}
