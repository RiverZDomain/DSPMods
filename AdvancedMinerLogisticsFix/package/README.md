# Advanced Miner Logistics Fix

Clears stale local and remote logistics orders when an Advanced Mining Machine's vein-collector storage slot changes item. It waits until the previous item count reaches zero before changing the slot's item ID and collection filter, then recalculates station needs. It also removes the collector's minimum local-drone dispatch threshold, allowing ILS/PLS drones to pick up a single item without reducing their actual carry capacity.

This companion patch is intended for mods that change miner output IDs. It does not randomize items or change miner output quantities, and regular stations retain vanilla dispatch behavior.

## Dependencies

- BepInEx 5