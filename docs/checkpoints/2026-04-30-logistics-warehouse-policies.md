# 2026-04-30 Logistics Warehouse Policies Checkpoint

## Summary

Continued from the synced `agent/economy-depth-pass` branch onto `agent/logistics-warehouse-policies`. The prototype now has a first-pass warehouse policy layer: each tracked market good exposes safety stock, reorder point, shipment priority, and a short policy action, while route contracts and automatic logistics use those priorities and avoid draining source warehouses below their safety reserve.

## Changed Systems

- `GodotBridge`: extended `PrototypeMarketSignal` with safety stock, reorder point, shipment priority, and policy action.
- `GodotBridge`: extended `PrototypeRouteContractView` with contracted units, shipment priority, and policy action.
- `GodotBridge`: automatic logistics now ranks candidate cargo by destination shipment priority before scarcity and keeps source warehouse safety reserves.
- `GodotBridge`: route contract generation now hides non-exportable stock, sorts by shipment priority before net value, and reserves only exportable units.
- `ChartersOfTrade.Godot`: city inspectors, priority warnings, route demand text, and contract labels now expose policy action, priority, units, safety, and reorder context.
- `Tests`: added coverage for warehouse policy signal thresholds and route-contract priority ordering.

## Tests

- `powershell -ExecutionPolicy Bypass -File .\tools\test.ps1`: passed with 22/22 tests and `INTERACTION_SMOKE PASS`.
- `powershell -ExecutionPolicy Bypass -File .\tools\benchmark.ps1`: passed with 25/25 playable seeds, average unmet demand ratio 0.7055, median time to profit 1.0, and bankruptcy frequency 0/25 after 12 ticks.

## Notes

- The branch is stacked on `agent/economy-depth-pass`, not directly on `main`, because it depends on `PrototypeMarketSignal`.
- Safety reserves intentionally lower short-run route extraction; the benchmark still stays playable with no bankrupt seeds.
- This is still automatic policy behavior; explicit player controls for reorder/safety settings remain future work.

## Next Step

Either prepare the stacked branches for review/merge in order, or continue into explicit warehouse automation controls in Godot.
