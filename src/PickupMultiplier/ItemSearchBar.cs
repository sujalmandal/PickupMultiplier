using System.Collections.Generic;
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
    private const float TitleHeight = 60f;
    private const float BarGap = 8f;
    private const float BarHeight = 56f;

    private static readonly HashSet<string> ItemCategories = new() { "Ammo", "Resources", "Food", "Tools" };
    private static readonly Dictionary<string, bool> CategoryVisibility = new();

    private static SettingsEntry _settingsEntry;
    private static SContainerOptions _bar;
    private static STextboxOptions _input;
    private static SLabelOptions _counter;
    private static string _query = string.Empty;
    private static bool _created;
    private static bool _roomMade;

    public static void OnSettingsPanelOpened(SettingsEntry settingsEntry)
    {
        _settingsEntry = settingsEntry;
        if (!_created)
        {
            BuildBar();
            _created = true;
        }

        Attach();
        ApplyQuery();
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
            .FontSize(16)
            .PWidth(150f)
            .Alignment(TextAlignmentOptions.MidlineRight)
            .FontColor(new Color(1f, 1f, 1f, 0.45f));

        _input = STextbox.Placeholder("Search items...").FlexWidth(1f).Notify(OnQueryChanged);

        var row = SContainer.Horizontal(10f, "CE").Dock(EDockType.Fill);
        row.Add(_input);
        row.Add(_counter);

        _bar = SContainer
            .Dock(EDockType.Top)
            .Size(-240f, BarHeight)
            .Position(0f, -(TitleHeight + BarGap + BarHeight / 2f))
            .Background(new Color(0f, 0f, 0f, 0.35f), EBackground.Round8, Image.Type.Sliced);
        _bar.Add(row);
    }

    private static void Attach()
    {
        var panel = GetPanel(PanelId);
        if (panel == null || _bar == null)
        {
            return;
        }

        MakeRoomForBar(panel);

        if (_bar.Root.transform.parent == panel.Root.transform)
        {
            return;
        }

        _bar.SetParent(panel.Root.transform);
    }

    private static void MakeRoomForBar(SPanelOptions panel)
    {
        if (_roomMade)
        {
            return;
        }

        var scrollRect = panel.Root.GetComponentInChildren<ScrollRect>(true);
        if (scrollRect == null)
        {
            return;
        }

        var target = scrollRect.transform;
        while (target.parent != null && target.parent != panel.Root.transform)
        {
            target = target.parent;
        }

        if (!(target is RectTransform rect))
        {
            return;
        }

        var offsetMax = rect.offsetMax;
        var wanted = Mathf.Abs(offsetMax.y) + BarGap * 2f + BarHeight;
        rect.offsetMax = new Vector2(offsetMax.x, -wanted);
        _roomMade = true;
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
}
