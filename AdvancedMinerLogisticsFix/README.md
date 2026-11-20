# Advanced Miner Logistics Fix

Clears stale local and remote logistics orders when an Advanced Mining Machine's vein-collector storage slot changes item. It waits until the previous item count reaches zero before changing the slot's item ID and collection filter, then recalculates station needs. It also removes the collector's minimum local-drone dispatch threshold, allowing ILS/PLS drones to pick up a single item without reducing their actual carry capacity. This is intended as a companion patch for mods that change miner output IDs, including Output Randomizer.

The mod does not randomize items or change miner output quantities. The dispatch adjustment only applies during an Advanced Mining Machine collector's local logistics tick; regular stations retain vanilla behavior.

## Build on Linux

Install the .NET SDK, DSP, and BepInEx 5. From this folder, run:

```sh
dotnet build AdvancedMinerLogisticsFix.csproj \
  -p:DSPGameDir="$HOME/.local/share/Steam/steamapps/common/Dyson Sphere Program" \
  -p:BepInExDir="$HOME/.local/share/Steam/steamapps/common/Dyson Sphere Program/BepInEx/core"
```

The compiled plugin is written to `bin/Debug/net472/AdvancedMinerLogisticsFix.dll`, and the Thunderstore-ready archive is written to `artifacts/AdvancedMinerLogisticsFix-0.1.0.zip`.