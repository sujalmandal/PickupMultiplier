# Pickup Multiplier

Multiplies the amount of items you receive from **world pickups** in *Sons of the Forest* — sticks, stones, resin, cloth, duct tape, berries, mushrooms, seeds, hides, feathers, ammo and other regular resources.

Unique, story and quest items are never multiplied.

## Features

- Configurable multiplier: **1 – 100** (default 100, the maximum)
- **A slider for every regular item**, in collapsible groups — so each item can have its own multiplier
- **Search box** above the item list: filter rows by name (`stick`), internal name (`i9mmammo`) or item id (`392`)
- Enable / disable toggle
- Optional item-id exclusion list
- Automatically skips unique, story and quest items — keycards, notes, email printouts, blueprint pages, artifacts, weapons, tools, masks
- Crafting, container loot and quest rewards are left untouched
- Settings apply instantly, no restart required

## Requirements

- Sons of the Forest 1.0.0
- [RedLoader](https://github.com/ToniMacaroni/RedLoader) 0.8.6 or newer

## Installation

Manual install: extract the `mods` folder in this archive into your game folder, i.e. into
`...\Steam\steamapps\common\Sons Of The Forest\` so that you end up with:

```
mods/PickupMultiplier.dll
mods/PickupMultiplier/manifest.json
```

Vortex: install the archive with the mod manager, then deploy as usual.

## Settings

In game: **pause menu → MODS → Pickup Multiplier → Settings**

| Setting | What it does |
| --- | --- |
| Enabled | Turns the whole mod on or off |
| Multiplier | Global slider, 1 – 100 (1 = vanilla behaviour, 100 = maximum) |
| Excluded item ids | Comma separated item ids that must never be multiplied |
| Search | Filters the item rows as you type; matches label, internal name or item id, and shows a count of matches. Global rows stay visible, and the box clears when the panel closes |
| Item sliders | One per regular item, in four collapsible groups: **Ammo**, **Resources**, **Food**, **Tools**. Rows start at the global value and follow it; set one to **0 to disable that item**, or to any value 1 – 100 for its own multiplier |

A per-item value always wins over the global one, and every slider has a Revert button that returns it
to following the global multiplier. The item groups open with a reminder of that: *0 = disabled,
otherwise follows the global multiplier*.

The settings are stored in `UserData/PickupMultiplier.cfg` — only the items you actually tuned are
written there:

```toml
[PickupMultiplier]
enabled = true
multiplier = 100
excludedItemIds = ""

[Ammo]
i9mmammo = 25
crossbowammobolt = 5

[Resources]
stick = 0
```

Here 9mm ammo and crossbow bolts have their own multipliers, and sticks are disabled, while every other
item follows the global 100.

`UserData/PickupMultiplier.status.txt` keeps a short log of the last 20 multiplied pickups
(`itemId name picked N -> granted M`), which is handy for checking what counted as a regular pickup.

## How it decides what to multiply

For each pickup the mod reads the item's `ItemData` and requires all of:

- the item is not unique per instance
- its max amount is greater than 1
- it has none of the `UniqueItem`, `Special` or `StructureBlueprint` type flags
- its id is not in your exclusion list

That is why one-off story items, weapons, tools and notes stay vanilla while ordinary resources
and ammo scale with the multiplier.

## Notes

- Item stack caps still apply. If a resource caps at 20 in your game, a x100 pickup will still
  leave you at 20 unless another mod raises the cap.
- The mod adds items through the inventory manager, which does not re-raise the pickup event, so
  it cannot recurse or double count.
- Client-side mod, safe to add or remove at any time. Removing it does not affect your save.
- Multiplayer is untested.

## Building from source

The build needs the .NET 6 SDK and a local Sons of the Forest install (the game's RedLoader assemblies are
used as references):

```bash
GAME="/path/to/Sons Of The Forest" ./build.sh            # build only
GAME="/path/to/Sons Of The Forest" ./build.sh --deploy   # build and copy into mods/
GAME="/path/to/Sons Of The Forest" ./package.sh          # deploy and zip dist/
```

`build.sh` refuses to deploy while `SonsOfTheForest.exe` is running, because a loaded DLL cannot be
replaced.

The per-item settings fields are generated, not handwritten:

- `tools/item-identifiers.txt` — the SDK's item table (346 ids), dumped from `SonsSdk.dll`
- `tools/item-groups.json` — group, label and include/exclude for every item
- `tools/generate-settings.py` — emits `src/PickupMultiplier/PickupMultiplierSettings.Generated.cs`
  (`--seed` regenerates the group JSON, `--all` also emits story items)
- `tools/InspectInterop` — the metadata dumper used to read item ids, item type flags and inventory APIs
  out of the game assemblies

## Credits

Built on [RedLoader](https://github.com/ToniMacaroni/RedLoader) and its Sons SDK by Toni Macaroni.
