# Nexus Mods page copy — Pickup Multiplier 2.1.1

Draft for review. Items marked **[your call]** need your decision before publishing.

## Title

Pickup Multiplier

## Summary (short description)

Multiplies the amount of regular items you pick up — sticks, resin, cloth, berries, ammo and more.
Unique, story and quest items stay vanilla.

## Category

Gameplay

Suggested tags: `Gameplay`, `Cheats`, `Quality of Life`, `Singleplayer`, `No Requirements`

## Description (body)

Paste the contents of `dist/README.md`, which is written as the mod description:

- Features
- Requirements (Sons of the Forest 1.0.0 + RedLoader)
- Installation (manual and Vortex)
- Settings (pause menu → MODS → Pickup Multiplier → Settings)
- How it decides what to multiply
- Notes, credits

## Permissions — DECIDED

The "credit-me" set. To be selected on the Nexus upload form:

- **Upload to other sites**: *No* — do not re-upload; link to this page instead.
- **Modification**: *Yes, with credit* — others may modify and improve, crediting this mod.
- **Asset use**: *Yes, with credit* — no custom assets exist in this archive, only the compiled mod and manifest.
- **Conversion to other games**: *No*.
- **Other**: contact the author first.

## AI disclosure

The upload form asks about AI-generated content. This mod's code was written with AI assistance,
reviewed and tested by the author. The disclosure field will be filled in honestly — not left blank.

## Authorization

The author has authorized the agent to complete the upload flow on their behalf, including accepting
Nexus's file submission guidelines and clicking Publish (2026-10-10).


## Version / file

- Version: `2.1.1`
- File: `PickupMultiplier-2.1.1.zip`
- File category: Main files
- Archive layout (matches the Nexus/Vortex convention for SOTF RedLoader mods):

```
mods/PickupMultiplier.dll
mods/PickupMultiplier/manifest.json
README.md
```

## Changelog

```
2.1.1
- Removed the item search box: it did not behave reliably for the value it added
- Kept the placeholder-label repair and whole-number slider readouts

2.1.0
- A slider for every regular item, grouped into collapsible Ammo, Resources, Food and Tools sections.
  Item rows start at the global value and follow it; 0 disables that item; 1 - 100 sets its own multiplier.
- Only items you actually change are written to the config, so the file stays short.
- Item ids come from the SDK's own item table (346 items), and labels are generated from it.

2.0.0
- Renamed to Pickup Multiplier.
- Covers all regular pickups, not just ammo: resources, food, materials and ammo.
- Unique, story and quest items are now excluded automatically, using the game's own item data
  (per-instance uniqueness, max amount, and the UniqueItem / Special / StructureBlueprint flags).
- Added a RedLoader settings panel: enable/disable toggle, 1-100 multiplier slider, and an
  item-id exclusion box. Settings apply instantly.
- Config is now managed by RedLoader in UserData/PickupMultiplier.cfg.

1.0.0
- Initial release: ammo pickups multiplied, configured through a text config file.
```

## Before publishing — checklist

1. ~~Permissions, AI disclosure and category chosen~~ — done, see above.
2. ~~Publish authority~~ — the author authorized the agent to submit.
3. v2.1.1 loaded once in game and confirmed working (**still pending** — the previous build crashed and
   was fixed; the repaired build has not been launched yet).
4. BrowserOS neo running and signed in to Nexus (**currently not running**).
5. Then: create the page, attach `PickupMultiplier-2.1.1.zip`, accept the submission guidelines, Publish.

## In-game test script (before upload)

1. Launch, load the save, then open **pause menu → MODS** — the card should read *Pickup Multiplier*
   2.1.1 with a **Settings** button.
2. Open Settings: expect the "Pickup multiplier" header, an **Enabled** checkbox, a **Multiplier**
   slider (1–100, sitting at 100), an **Excluded item ids** box, then the four collapsible item groups
   (Ammo, Resources, Food, Tools) with a slider per item. Item rows start at the global value; 0 disables
   that item.
3. Pick up a stick or a berry — the inventory count should jump by x100 (clamped by the item's cap).
4. Pick up a keycard, note or blueprint — it must stay exactly 1.
5. Fire a weapon — ammo should decrease (infinite ammo is off in FrankyModMenu).
6. Quit, and the agent reads `UserData/PickupMultiplier.status.txt` to confirm which pickups were multiplied.

