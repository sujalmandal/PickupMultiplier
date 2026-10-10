using System.Collections.Generic;
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

    private static readonly HashSet<string> ItemCategories = new() { "Ammo", "Resources", "Food", "Tools" };
    private static readonly Dictionary<string, bool> CategoryVisibility = new();

    private static SettingsEntry _settingsEntry;
    private static SContainerOptions _bar;
    private static STextboxOptions _input;
    private static SLabelOptions _counter;
    private static string _query = string.Empty;
    private static bool _created;
    private static float? _contentTopInset;
    private static string _debugPath;

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
            return;
        }

        var panelHeight = panelRect.rect.height;
        var panelWidth = panelRect.rect.width;
        if (panelHeight <= 1f || panelWidth <= 1f)
        {
            return;
        }

        var barHeight = Mathf.Clamp(panelHeight * BarHeightRatio, MinBarHeight, MaxBarHeight);
        var gap = Mathf.Clamp(panelHeight * GapRatio, MinGap, MaxGap);
        var titleHeight = Mathf.Clamp(panelHeight * TitleHeightRatio, MinTitleHeight, MaxTitleHeight);
        var counterWidth = Mathf.Clamp(panelWidth * CounterWidthRatio, MinCounterWidth, MaxCounterWidth);
        var fontSize = Mathf.RoundToInt(Mathf.Clamp(panelHeight * FontSizeRatio, MinFontSize, MaxFontSize));

        _counter.PWidth(counterWidth).FontSize(fontSize);

        var left = 0f;
        var right = 0f;
        var bandTop = titleHeight + gap;

        if (scroll != null)
        {
            if (_contentTopInset == null)
            {
                _contentTopInset = scroll.offsetMax.y;
            }

            left = scroll.offsetMin.x;
            right = scroll.offsetMax.x;
            bandTop = Mathf.Max(Mathf.Abs(_contentTopInset.Value), titleHeight + gap);
            scroll.offsetMax = new Vector2(right, -(bandTop + barHeight + gap));
        }

        var barRect = _bar.RectTransform;
        barRect.anchorMin = new Vector2(0f, 1f);
        barRect.anchorMax = new Vector2(1f, 1f);
        barRect.pivot = new Vector2(0.5f, 1f);
        barRect.offsetMin = new Vector2(left, -(bandTop + barHeight));
        barRect.offsetMax = new Vector2(right, -bandTop);
    }

    // SUI reuses the game's option prefabs, and a reused label can still carry the game's placeholder
    // text, so every divider and title that shows it gets the text it should have had
    private static void RepairPlaceholderText(SPanelOptions panel, RectTransform scroll)
    {
        var categories = ResolveCategoryOrder();

        if (scroll != null)
        {
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
                if (label == null || !IsPlaceholder(label.text))
                {
                    continue;
                }

                var category = i < categories.Count ? categories[i] : "Items";
                label.text = $"\uf0d7 {category}";
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
            if (entry == null || entry.ConfigEntry == null)
            {
                continue;
            }

            var slider = _settingsEntry.GetWrappedElementForEntry(entry.ConfigEntry)?.As<SSliderOptions>();
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

        return Contains(configEntry.DisplayName, query)
            || Contains(configEntry.Identifier, query)
            || Contains(configEntry.Description, query);
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

        var lines = new List<string>();
        var panelRect = panel.Root.GetComponent<RectTransform>();
        var barRect = _bar.RectTransform;

        lines.Add($"mod          {PickupMultiplierMod.ModId}");
        lines.Add($"panel rect   {Describe(panelRect)}");
        lines.Add($"scroll rect  {Describe(scroll)}");
        lines.Add($"content top  {_contentTopInset}");
        lines.Add($"bar rect     {Describe(barRect)}");
        lines.Add("");
        lines.Add("panel children:");

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
            lines.Add("content children:");
            foreach (Transform child in scroll)
            {
                lines.Add($"  {child.name} {TextOf(child)}");
            }
        }

        File.WriteAllLines(_debugPath, lines);
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
