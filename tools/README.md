# DSP Assembly Inspection Tools

These small Mono.Cecil console tools inspect the installed game's `Assembly-CSharp.dll` metadata and IL. They do not modify the game assembly.

## Prerequisites

- .NET SDK 10 or later
- DSP installed
- BepInEx 5 installed through r2modman (the tools use its `Mono.Cecil.dll`)

By default, the project files look for Mono.Cecil in the r2modman Default profile. If yours differs, pass `-p:BepInExDir="/path/to/BepInEx/core"` to `dotnet run`.

## Run

From the DPSMods repository root:

```sh
GAME_ASSEMBLY="$HOME/.local/share/Steam/steamapps/common/Dyson Sphere Program/DSPGAME_Data/Managed/Assembly-CSharp.dll"

dotnet run --project tools/MinerInspect/MinerInspect.csproj -- "$GAME_ASSEMBLY"
dotnet run --project tools/MenuInspect/MenuInspect.csproj -- "$GAME_ASSEMBLY"
```

`MinerInspect` reports miner and collector methods, station storage fields, logistics enums, and relevant component-creation call sites. `MenuInspect` reports the title-menu lifecycle methods, menu button fields, and `UIRoot` open/close methods.