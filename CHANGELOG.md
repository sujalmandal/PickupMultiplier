# Changelog

## 2.1.0

- A slider for every regular item, grouped into collapsible **Ammo**, **Resources**, **Food** and **Tools** sections
- **Search box** above the item list: filters rows by name, internal name, item id or group as you type
- `0` disables an item; rows otherwise follow the global multiplier, and Revert returns a row to following it
- Per-item settings are generated from the SDK's own item table (`tools/generate-settings.py`), and only items
  you actually change are written to the config
- Settings page is laid out from the panel's real rect rather than fixed offsets, so it holds up across
  resolutions and aspect ratios
- Slider readouts are forced to whole numbers, and reused prefab labels showing placeholder text are repaired
- Each time the settings page opens, the mod writes `UserData/PickupMultiplier.debug.txt` with the panel
  geometry, divider labels and slider readouts, so a layout problem can be diagnosed from a file

## 2.0.0

- Renamed to **Pickup Multiplier**
- Covers all regular pickups, not just ammo: resources, food, materials and ammo
- Unique, story and quest items are excluded automatically, using the game's own item data
- Added the RedLoader settings panel: enable toggle, 1–100 multiplier slider, item-id exclusion box
- Config is managed by RedLoader in `UserData/PickupMultiplier.cfg`

## 1.0.0

- Initial release: ammo pickups multiplied, configured through a text config file
