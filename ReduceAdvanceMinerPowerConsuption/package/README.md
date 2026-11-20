# Advanced Miner Linear Power

Advanced vein collector power demand scales linearly with miner speed instead of quadratically. Existing collectors are registered and updated in a one-time scan when the save loads; newly placed collectors are registered when DSP creates their miner components. Power updates are handled through a Harmony postfix, with no recurring factory scan. Base power and regular miners are unchanged.

Vanilla ratio: `speedDamper * speed^2 / 100,000,000`.

Mod ratio: `speedDamper * speed / 10,000`.

## Install

Install DSP BepInEx 5, then install this package with a Thunderstore-compatible mod manager. For manual installation, extract the package into the Dyson Sphere Program game directory so the plugin is located at `BepInEx/plugins/AdvancedMinerLinearPower/AdvancedMinerLinearPower.dll`.

## Configuration

No configuration is required.