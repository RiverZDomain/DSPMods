# DistributeSpray MK3 (Personal Modification)

## Attribution and Scope

This is an unofficial, personal-use modification of **DistributeSpray**, originally created by Valoneu and included in the [Valoneu/DysonSphereMods repository](https://github.com/Valoneu/DysonSphereMods/tree/main/src/DistributeSpray). The original mod and repository are not mine. This modified version is for my own use, is not an official Valoneu release, and is not affiliated with or endorsed by Valoneu. The changes here do not claim ownership of the original work. Consult the upstream project for its terms before redistributing the original or modified code.

Automatically applies MK3 proliferator (item ID `1143`) coating to items as they are inserted into machines, storage, and logistics stations. The coating increment is `4`. The configured `ProlSpray` value is `240`, so one MK3 proliferator pays for 240 spray-debt units. MK1 and MK2 proliferators are ignored and never consumed.

Set station slots containing MK3 proliferator to `Storage` (`None`) so the mod can consume them. `General.RemoveProliferatorPowerSurcharge` defaults to `true`; it removes proliferator-related extra power demand from assemblers and labs without removing their coating bonuses. It affects all proliferator-coated production in those buildings while the mod is enabled. Set it to `false` to retain vanilla power costs. The master toggle is `General.Enabled`.

## Dependencies

- BepInEx 5