#!/usr/bin/env python3

import argparse
import json
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
IDENTIFIERS_PATH = os.path.join(ROOT, "tools", "item-identifiers.txt")
GROUPS_PATH = os.path.join(ROOT, "tools", "item-groups.json")
OUTPUT_PATH = os.path.join(ROOT, "src", "PickupMultiplier", "PickupMultiplierSettings.Generated.cs")

GROUP_IDS = ["Ammo", "Resources", "Food", "Tools"]
ITEM_LEGEND = "0 = disabled, otherwise follows the global multiplier"

STORY_PATTERN = re.compile(
    r"(Blueprint|KeyCard|Artifact|Pamphlet|Notice|Printout|Note|TableCard|BunkerMap|Tutorial|Template"
    r"|CultSign|Holoville|Newspaper|Email|StoryPages|AlbumCover|Bible|Numbers|StockPrice|Jianyu"
    r"|OldNote|PersonalNote|Novel|BunkerMapA)",
    re.IGNORECASE,
)
AMMO_PATTERN = re.compile(r"(Ammo|Arrow|Bolt|Dart|Flare|Molotov|Grenade|Bomb|Slug|Buckshot)", re.IGNORECASE)
FOOD_PATTERN = re.compile(
    r"(Berry|Berries|Seed|Mushroom|Meat|Fish|Food|Steak|Ramen|Cereal|Oyster|Egg|Mre|Energy|Pills|Meds"
    r"|Aloe|Chicory|Horsetail|Yarro|Fireweed|DevilsClub|Arrowleaf|Guarana|Salmon|Snowberries|Twinberries"
    r"|Blackberries|Blueberries|CatFood|Canned|BrainBite|BaconBite|Vodka|HealthMix|Water)",
    re.IGNORECASE,
)
TOOL_PATTERN = re.compile(
    r"(Pistol|Rifle|Shotgun|Revolver|Bow|Crossbow|Katana|Machete|Axe|Knife|Spear|Club|Sled|Glider|Suit"
    r"|Jacket|Pants|Boots|Armour|Goggles|Rebreather|Flippers|Mask|Watch|Binoculars|Camera|Radio|Walkie"
    r"|Torch|Lighter|Chainsaw|Shovel|Pickaxe|StunGun|Airgun|Slingshot|Taser|Charger|Backpack|LootPouch"
    r"|RopeGun|RepairTool|LaserMeasure|Gps|Locator|Putter|Golf|Mod|Rail|Suppressor|Scope|Quiver|Guitar"
    r"|GrabBag|CanOpener|Flask|Pot|SleepingBag|Pyjamas|Hoodie|Tuxedo|Dress|Outfit|Shirt|Camo|Jeans"
    r"|Flashlight|KnightV|EmergencyPack)",
    re.IGNORECASE,
)
WORD_FIXES = {
    "3d": "3D",
    "gps": "GPS",
    "mre": "MRE",
    "c4": "C4",
    "ui": "UI",
    "id": "ID",
    "mm": "mm",
    "9mm": "9mm",
    "hp": "HP",
    "vip": "VIP",
    "nv": "NV",
}


def read_identifiers():
    rows = []
    with open(IDENTIFIERS_PATH, encoding="utf-8") as handle:
        for line in handle:
            parts = line.split()
            if len(parts) != 2 or not parts[1].isdigit():
                continue
            rows.append({"name": parts[0], "id": int(parts[1])})
    if not rows:
        raise SystemExit(f"no identifiers found in {IDENTIFIERS_PATH}")
    return rows


def resolve_group(name):
    if STORY_PATTERN.search(name):
        return "Story"
    if AMMO_PATTERN.search(name):
        return "Ammo"
    if FOOD_PATTERN.search(name):
        return "Food"
    if TOOL_PATTERN.search(name):
        return "Tools"
    return "Resources"


def group_of(item):
    return item["group"] if item["group"] in GROUP_IDS else "Resources"


def build_label(name):
    stripped = name
    if len(stripped) > 1 and stripped[0] == "I" and stripped[1].isdigit():
        stripped = stripped[1:]

    words = re.findall(r"[A-Z]+(?![a-z])|[A-Z][a-z0-9]*|[a-z0-9]+", stripped)
    if not words:
        return name

    merged = []
    for word in words:
        if merged and len(merged[-1]) == 1 and merged[-1].isupper() and word.isdigit():
            merged[-1] = merged[-1] + word
            continue
        merged.append(word)
    words = merged

    label_words = []
    for index, word in enumerate(words):
        fixed = WORD_FIXES.get(word.lower())
        if fixed is not None:
            label_words.append(fixed)
            continue
        label_words.append(word if index == 0 and word[0].isdigit() else word.capitalize())
    return " ".join(label_words)


def seed_groups():
    items = []
    for row in read_identifiers():
        group = resolve_group(row["name"])
        items.append({
            "id": row["id"],
            "name": row["name"],
            "label": build_label(row["name"]),
            "group": group,
            "include": group != "Story",
        })
    items.sort(key=lambda item: item["id"])
    document = {"groups": GROUP_IDS, "items": items}
    with open(GROUPS_PATH, "w", encoding="utf-8") as handle:
        json.dump(document, handle, indent=2)
        handle.write("\n")
    included = sum(1 for item in items if item["include"])
    print(f"seeded {len(items)} items ({included} included) into {GROUPS_PATH}")


def load_groups():
    with open(GROUPS_PATH, encoding="utf-8") as handle:
        document = json.load(handle)
    items = document["items"]
    seen_ids = {}
    for item in items:
        if item["id"] in seen_ids:
            raise SystemExit(f"duplicate id {item['id']} ({item['name']} / {seen_ids[item['id']]})")
        if item["group"] not in GROUP_IDS and item["include"]:
            raise SystemExit(f"unknown group {item['group']!r} for {item['name']}")
        seen_ids[item["id"]] = item["name"]
    return document


def write_generated(document, include_story):
    items = [item for item in document["items"] if item["include"] or include_story]
    items.sort(key=lambda item: (GROUP_IDS.index(group_of(item)), item["id"]))

    lines = [
        "// generated by tools/generate-settings.py - do not edit by hand",
        "using System.Collections.Generic;",
        "using RedLoader;",
        "using SonsSdk.Attributes;",
        "",
        "namespace PickupMultiplier;",
        "",
        "public partial class PickupMultiplierSettings",
        "{",
    ]

    seen_groups = set()
    for item in items:
        group = group_of(item)
        if group not in seen_groups:
            seen_groups.add(group)
            lines.append(f'    [SettingsUiHeader("{group} - {ITEM_LEGEND}")]')
        lines.append("    [SettingsUiInclude]")
        lines.append(f"    public ConfigEntry<int> {group}_{item['name']};")
    lines.append("")

    lines.append("    public void RegisterItemEntries(Dictionary<int, ConfigEntry<int>> itemEntries, int globalMultiplier)")
    lines.append("    {")
    for group in GROUP_IDS:
        lines.append(f'        var {group.lower()} = ConfigSystem.CreateFileCategory("{group}", "{group}", FileName);')
    lines.append("")
    for item in items:
        group = group_of(item)
        field = f"{group}_{item['name']}"
        description = f"0 = disabled, otherwise follows the global multiplier ({item['id']}, {item['name']})"
        lines.append(
            f'        {field} = RegisterItem({group.lower()}, itemEntries, {item["id"]}, '
            f'"{item["name"].lower()}", "{item["label"]}", "{description}", globalMultiplier);'
        )
    lines.append("    }")
    lines.append("}")

    with open(OUTPUT_PATH, "w", encoding="utf-8") as handle:
        handle.write("\n".join(lines) + "\n")
    print(f"wrote {len(items)} item entries to {OUTPUT_PATH}")


def main():
    parser = argparse.ArgumentParser(description="Generate the per-item settings fields for PickupMultiplier")
    parser.add_argument("--seed", action="store_true", help="rewrite tools/item-groups.json from the dump")
    parser.add_argument("--all", action="store_true", help="also emit entries currently marked as story items")
    args = parser.parse_args()

    if args.seed:
        seed_groups()
        return

    document = load_groups()
    write_generated(document, args.all)


if __name__ == "__main__":
    sys.exit(main())
