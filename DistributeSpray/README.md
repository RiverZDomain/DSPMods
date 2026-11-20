# DistributeSpray MK3 (Personal Modification)

## Attribution and Scope

This is an unofficial, personal-use modification of **DistributeSpray**, originally created by Valoneu and included in the [Valoneu/DysonSphereMods repository](https://github.com/Valoneu/DysonSphereMods/tree/main/src/DistributeSpray). The original mod and repository are not mine. This modified version is for my own use, is not an official Valoneu release, and is not affiliated with or endorsed by Valoneu. The changes here do not claim ownership of the original work. Consult the upstream project for its terms before redistributing the original or modified code.

Automatically applies proliferator spray to items entering machines, using proliferator stored in logistics stations. This version uses MK3 proliferator only (item ID `1143`), applying coating increment `4`. The configured `ProlSpray` value is `240`, so one MK3 proliferator pays for 240 spray-debt units. MK1 and MK2 proliferators are ignored and never consumed.

Station storage slots must use the `Storage` (`None`) logistics mode for the mod to draw from them. `General.RemoveProliferatorPowerSurcharge` defaults to `true`; it removes proliferator-related extra power demand from assemblers and labs without removing their coating bonuses. This affects all proliferator-coated production in those buildings while the mod is enabled. Set it to `false` to retain vanilla power costs. The master toggle is `General.Enabled`.

## Build on Linux

Install the .NET SDK, DSP, and BepInEx 5. From this folder, run:

```sh
dotnet build DistributeSpray.csproj \
  -p:DSPGameDir="$HOME/.local/share/Steam/steamapps/common/Dyson Sphere Program" \
  -p:BepInExDir="$HOME/.local/share/Steam/steamapps/common/Dyson Sphere Program/BepInEx/core"
```

The compiled plugin is written to `bin/Debug/net472/DistributeSpray.dll`, and the Thunderstore-ready archive is written to `artifacts/DistributeSpray-1.0.4.zip`.
