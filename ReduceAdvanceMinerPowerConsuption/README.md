# Advanced Miner Linear Power

Changes advanced vein collector power use from quadratic to linear scaling with mining speed. The mod scans and updates collectors in the loaded save once at game start, then registers newly placed collectors when DSP creates their miner components. A Harmony postfix applies the formula whenever DSP updates a registered miner's power state. There is no recurring factory scan. Base power is left unchanged, and regular miners are unaffected.

Vanilla uses `speedDamper * speed^2 / 100,000,000`. The mod uses `speedDamper * speed / 10,000`, preserving the base demand at speed 10,000 while making demand grow linearly above that speed.

## Build on Linux

Install the .NET SDK, DSP, and BepInEx 5. From this folder, run:

```sh
dotnet build ReduceAdvanceMinerPowerConsuption.csproj \
  -p:DSPGameDir="$HOME/.local/share/Steam/steamapps/common/Dyson Sphere Program" \
  -p:BepInExDir="$HOME/.local/share/Steam/steamapps/common/Dyson Sphere Program/BepInEx/core"
```

The build creates `artifacts/AdvancedMinerLinearPower-0.1.0.zip`. If your Steam library or BepInEx installation is elsewhere, pass the matching paths.# Reduce Advanced Miner Power Consumption

Mod source and build files for this project belong in this folder.