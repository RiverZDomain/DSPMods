# Output Randomizer

Randomizes what miners produce. Instead of always outputting the vein's native resource, miners will randomly produce any item you allow.

## Usage

On the **New Game** screen, a **Randomizer** panel appears below the metadata table:

- **Random Miner Outputs** — toggle to enable/disable for that save
- **Filter** — opens a grid where you select which items can appear (bright = included, gray = excluded)

Settings are saved per save file. The filter requires at least one item selected.

Affects all miners: vein miners, water pumps, and oil extractors.

## Installation

1. Install [BepInEx 5](https://github.com/BepInEx/BepInEx/releases) into your game folder
2. Drop the DLL into `BepInEx/plugins`