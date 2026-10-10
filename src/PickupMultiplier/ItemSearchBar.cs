using System.Collections.Generic;
using System.Globalization;
using System.IO;
using RedLoader;
using SUI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SettingsConfigEntry = SUI.SettingsRegistry.SettingsConfigEntry;
using SettingsEntry = SUI.SettingsRegistry.SettingsEntry;

using static SUI.SUI;

namespace PickupMultiplier;

public static class ItemSearchBar
{
    private const string PanelId = "ModSettingsPanel";
    private const string DebugFileName = "PickupMultiplier.debug.txt";
    private const string PlaceholderMarker = "IPSUM";
    private const string ExpandedGlyph = "\uf0d7";
    private const string CollapsedGlyph = "\uf0da";

    private const float BarHeightRatio = 0.052f;
    private const float MinBarHeight = 40f;
    private const float MaxBarHeight = 64f;
    private const float GapRatio = 0.008f;
    private const float MinGap = 6f;
    private const float MaxGap = 14f;
    private const float TitleHeightRatio = 0.056f;
    private const float MinTitleHeight = 50f;
    private const float MaxTitleHeight = 76f;
    private const float CounterWidthRatio = 0.08f;
    private const float MinCounterWidth = 110f;
    private const float MaxCounterWidth = 190f;
    private const float FontSizeRatio = 0.015f;
    private const int MinFontSize = 12;
    private const int MaxFontSize = 20;
    private const float FallbackWidthRatio = 0.35f;
    private const float MinFallbackWidth = 320f;
    private const float MaxFallbackWidth = 700f;

    private static readonly HashSet<string> ItemCategories = new() { "Ammo", "Resources", "Food", "Tools" };
    private static readonly Dictionary<string, bool> CategoryVisibility = new();
    private static readonly Dictionary<ConfigEntry, int> ItemIds = new();

    private static SettingsEntry _settingsEntry;
    private static SContainerOptions _bar;
    private static STextboxOptions _input;
    private static SLabelOptions _counter;
    private static string _query = string.Empty;
    private static string _placement = "unplaced";
    private static bool _created;
    private static float? _contentTopInset;
    private static string _debugPath;

    public static void SetItemMultipliers(Dictionary<int, ConfigEntry<int>> itemMultipliers)
    {
        ItemIds.Clear();
        foreach (var pair in itemMultipliers)
        {
            if (pair.Value != null)
            {
                ItemIds[pair.Value] = pair.Key;
            }
        }
    }

    public static void OnSettingsPanelOpened(SettingsEntry settingsEntry)
    {
        _settingsEntry = settingsEntry;
        _debugPath ??= ConfigSystem.GetFilePath(DebugFileName);

        var panel = GetPanel(PanelId);
        if (panel == null)
        {
            return;
        }

        if (!_created)
        {
            BuildBar();
            _created = true;
        }

        var scroll = FindScrollRoot(panel);
        Layout(panel, scroll);
        RepairPlaceholderText(panel, scroll);
        RefreshSliders();
        ApplyQuery();
        WriteDebugDump(panel, scroll);
    }

    public static void OnCategoryToggled(string identifier, bool visible)
    {
        CategoryVisibility[identifier] = visible;
        ApplyQuery();
    }

    public static void Reset()
    {
        _query = string.Empty;
        if (_input != null)
        {
            _input.Value(string.Empty);
        }

        ApplyQuery();
    }

    private static void BuildBar()
    {
        _counter = SLabel.Text(string.Empty)
            .Alignment(TextAlignmentOptions.MidlineRight)
            .FontColor(new Color(1f, 1f, 1f, 0.45f));

        _input = STextbox.Placeholder("Search items...").FlexWidth(1f).Notify(OnQueryChanged);

        var row = SContainer.Horizontal(10f, "CE").Dock(EDockType.Fill);
        row.Add(_input);
        row.Add(_counter);

        _bar = SContainer.Background(new Color(0f, 0f, 0f, 0.35f), EBackground.Round8, Image.Type.Sliced);
        _bar.Add(row);
        _bar.Active(false);
    }

    private static RectTransform FindScrollRoot(SPanelOptions panel)
    {
        var scrollRect = panel.Root.GetComponentInChildren<ScrollRect>(true);
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

    private static void Layout(SPanelOptions panel, RectTransform scroll)
    {
        if (_bar.Root.transform.parent != panel.Root.transform)
        {
            _bar.SetParent(panel.Root.transform);
        }

        var panelRect = panel.Root.GetComponent<RectTransform>();
        if (panelRect == null)
        {
            _bar.Active(false);
            _placement = "waiting for the panel";
            return;
        }

        var panelHeight = panelRect.rect.height;
        var panelWidth = panelRect.rect.width;
        if (panelHeight <= 1f || panelWidth <= 1f)
        {
            _bar.Active(false);
            _placement = "waiting for the panel rect";
            return;
        }

        var barHeight = Mathf.Clamp(panelHeight * BarHeightRatio, MinBarHeight, MaxBarHeight);
        var gap = Mathf.Clamp(panelHeight * GapRatio, MinGap, MaxGap);
        var titleHeight = Mathf.Clamp(panelHeight * TitleHeightRatio, MinTitleHeight, MaxTitleHeight);
        var counterWidth = Mathf.Clamp(panelWidth * CounterWidthRatio, MinCounterWidth, MaxCounterWidth);
        var fontSize = Mathf.RoundToInt(Mathf.Clamp(panelHeight * FontSizeRatio, MinFontSize, MaxFontSize));

        _counter.PWidth(counterWidth).FontSize(fontSize);

        var barRect = _bar.RectTransform;
        barRect.pivot = new Vector2(0.5f, 1f);

        if (scroll == null)
        {
            LayoutInBottomCorner(barRect, panelWidth, panelHeight, barHeight, gap);
            _placement = "bottom corner (no scroll container found)";
            _bar.Active(true);
            return;
        }

        if (_contentTopInset == null)
        {
            _contentTopInset = scroll.offsetMax.y;
        }

        var left = scroll.offsetMin.x;
        var right = scroll.offsetMax.x;
        var bandTop = Mathf.Max(Mathf.Abs(_contentTopInset.Value), titleHeight + gap);
        scroll.offsetMax = new Vector2(right, -(bandTop + barHeight + gap));

        barRect.anchorMin = new Vector2(0f, 1f);
        barRect.anchorMax = new Vector2(1f, 1f);
        barRect.offsetMin = new Vector2(left, -(bandTop + barHeight));
        barRect.offsetMax = new Vector2(right, -bandTop);
        _placement = $"above the list, insets {left} / {right}";
        _bar.Active(true);
    }

    private static void LayoutInBottomCorner(RectTransform barRect, float panelWidth, float panelHeight, float barHeight, float gap)
    {
        var width = Mathf.Clamp(panelWidth * FallbackWidthRatio, MinFallbackWidth, MaxFallbackWidth);
        var inset = Mathf.Clamp(panelWidth * CounterWidthRatio / 2f, gap, gap * 4f);
        var bottom = Mathf.Clamp(panelHeight * 0.07f, gap, gap * 6f);

        barRect.pivot = new Vector2(1f, 0f);
        barRect.anchorMin = new Vector2(1f, 0f);
        barRect.anchorMax = new Vector2(1f, 0f);
        barRect.offsetMin = new Vector2(-inset - width, bottom);
        barRect.offsetMax = new Vector2(-inset, bottom + barHeight);
    }

    // SUI reuses the game's option prefabs, and a reused label can still carry the game's placeholder text
    private static void RepairPlaceholderText(SPanelOptions panel, RectTransform scroll)
    {
        if (scroll != null)
        {
            var categories = ResolveCategoryOrder();
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
                if (Contains(label.text, category.ToLowerInvariant()))
                {
                    continue;
                }

                var glyph = label.text != null && label.text.StartsWith(CollapsedGlyph) ? CollapsedGlyph : ExpandedGlyph;
                label.text = $"{glyph} {category}";
            }
        }

        foreach (Transform child in panel.Root.transform)
        {
            var label = child.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label == null || !IsPlaceholder(label.text))
            {
                continue;
            }

            label.text = "PICKUP MULTIPLIER";
        }
    }

    private static bool IsPlaceholder(string text)
    {
        return !string.IsNullOrEmpty(text) && text.ToUpperInvariant().Contains(PlaceholderMarker);
    }

    private static List<string> ResolveCategoryOrder()
    {
        var order = new List<string>();
        if (_settingsEntry == null)
        {
            return order;
        }

        foreach (var entry in _settingsEntry.ConfigEntries)
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

    private static void RefreshSliders()
    {
        if (_settingsEntry == null)
        {
            return;
        }

        foreach (var entry in _settingsEntry.ConfigEntries)
        {
            var slider = ResolveSlider(entry);
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

    private static SSliderOptions ResolveSlider(SettingsConfigEntry entry)
    {
        if (entry == null || entry.ConfigEntry == null || _settingsEntry == null)
        {
            return null;
        }

        return _settingsEntry.GetWrappedElementForEntry(entry.ConfigEntry)?.As<SSliderOptions>();
    }

    private static void OnQueryChanged(string value)
    {
        _query = value ?? string.Empty;
        ApplyQuery();
    }

    private static void ApplyQuery()
    {
        if (!_created || _settingsEntry == null)
        {
            return;
        }

        var query = _query.Trim().ToLowerInvariant();
        var searching = query.Length > 0;
        var matches = 0;
        var total = 0;

        foreach (var entry in _settingsEntry.ConfigEntries)
        {
            if (entry == null || !ItemCategories.Contains(entry.CategoryName))
            {
                continue;
            }

            total++;
            var visible = !IsCategoryHidden(entry.CategoryName) && (!searching || Matches(entry, query));
            entry.ShouldBeVisible = visible;
            if (visible)
            {
                matches++;
            }
        }

        UpdateCounter(searching, matches, total);
    }

    private static void UpdateCounter(bool searching, int matches, int total)
    {
        if (_counter == null)
        {
            return;
        }

        _counter.Text(searching ? $"{matches} / {total}" : $"{total} items");
    }

    private static bool IsCategoryHidden(string identifier)
    {
        return CategoryVisibility.TryGetValue(identifier, out var visible) && !visible;
    }

    private static bool Matches(SettingsConfigEntry entry, string query)
    {
        var configEntry = entry.ConfigEntry;
        if (configEntry == null)
        {
            return false;
        }

        if (Contains(configEntry.DisplayName, query) || Contains(configEntry.Identifier, query))
        {
            return true;
        }

        if (Contains(entry.CategoryName, query))
        {
            return true;
        }

        return ItemIds.TryGetValue(configEntry, out var itemId)
            && itemId.ToString(CultureInfo.InvariantCulture).Contains(query);
    }

    private static bool Contains(string text, string query)
    {
        return !string.IsNullOrEmpty(text) && text.ToLowerInvariant().Contains(query);
    }

    private static void WriteDebugDump(SPanelOptions panel, RectTransform scroll)
    {
        if (string.IsNullOrEmpty(_debugPath))
        {
            return;
        }

        var lines = new List<string>
        {
            $"mod          {PickupMultiplierMod.ModId}",
            $"placement    {_placement}",
            $"panel rect   {Describe(panel.Root.GetComponent<RectTransform>())}",
            $"scroll rect  {Describe(scroll)}",
            $"bar rect     {Describe(_bar.RectTransform)}",
            $"content top  {_contentTopInset}",
            "",
            "panel children:"
        };

        foreach (Transform child in panel.Root.transform)
        {
            lines.Add($"  {child.name} active={child.gameObject.activeSelf} {TextOf(child)}");
        }

        if (scroll != null)
        {
            lines.Add("");
            lines.Add("dividers:");
            var index = 0;
            foreach (var candidate in scroll.GetComponentsInChildren<Transform>(true))
            {
                if (candidate.name != "Divider")
                {
                    continue;
                }

                lines.Add($"  [{index}] {TextOf(candidate)}");
                index++;
            }

            lines.Add("");
            lines.Add("first content children:");
            var shown = 0;
            foreach (Transform child in scroll)
            {
                if (shown++ >= 15)
                {
                    break;
                }

                lines.Add($"  {child.name} {TextOf(child)}");
            }
        }

        lines.Add("");
        lines.Add("item slider readouts (label | readout):");
        if (_settingsEntry != null)
        {
            foreach (var entry in _settingsEntry.ConfigEntries)
            {
                if (entry == null || !ItemCategories.Contains(entry.CategoryName))
                {
                    continue;
                }

                lines.Add($"  {entry.ConfigEntry?.DisplayName} = {TextsOf(ResolveSlider(entry))}");
            }
        }

        File.WriteAllLines(_debugPath, lines);
    }

    private static string TextsOf(SSliderOptions slider)
    {
        if (slider == null || slider.Root == null)
        {
            return "(no slider)";
        }

        var texts = new List<string>();
        foreach (var label in slider.Root.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            texts.Add($"\"{label.text}\"");
        }

        return string.Join(" | ", texts);
    }

    private static string Describe(RectTransform rect)
    {
        if (rect == null)
        {
            return "(missing)";
        }

        return $"anchors {rect.anchorMin}..{rect.anchorMax} offsets {rect.offsetMin}..{rect.offsetMax} size {rect.rect.size}";
    }

    private static string TextOf(Transform transform)
    {
        var label = transform.GetComponentInChildren<TextMeshProUGUI>(true);
        return label == null ? "text=(none)" : $"text=\"{label.text}\"";
    }
}
