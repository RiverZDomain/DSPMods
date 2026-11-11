# Blueprint Scaler Mod

The first release adds a draggable floating scale window while a blueprint is in placement mode. It starts at 200 and can be adjusted in steps of 10; the selected value is saved in the BepInEx configuration. Blueprint geometry scaling is not implemented yet.

## Install

Install the DSP BepInEx 5 package, then install this package with a Thunderstore-compatible mod manager. For manual installation, extract the package contents into the Dyson Sphere Program game directory so the plugin is located at `BepInEx/plugins/BlueprintScalerMod/BlueprintScalerMod.dll`.

## Build on Linux

Install the .NET SDK, DSP, and BepInEx 5. From the repository root, build and create a Thunderstore archive with:

```sh
dotnet build BlueprintScalerMod.csproj \
  -p:DSPGameDir="$HOME/.local/share/Steam/steamapps/common/Dyson Sphere Program" \
  -p:BepInExDir="$HOME/.local/share/Steam/steamapps/common/Dyson Sphere Program/BepInEx/core"
```

The archive is written to `artifacts/BlueprintScalerMod-0.1.0.zip`. If your Steam library or BepInEx installation is elsewhere, pass the matching paths. `BepInExDir` must point to the directory containing `BepInEx.dll` and `0Harmony.dll`.

## Test

Launch DSP with the package installed and open the blueprint inspector. A transient "blueprint menu opened" message should appear. The hook targets the inspector used for browser, copy, and paste workflows; it does not detect opening the separate full blueprint library window.