# 2026-04-30 Economy Depth Pass Checkpoint

## Summary

Started the post-merge economy depth phase on `agent/economy-depth-pass` from a synced `origin/main`. The prototype now exposes local per-city market pressure instead of relying only on the charter-town price view, and the Godot inspector/warning copy explains which goods are short, stocked out, stable, or surplus candidates.

## Changed Systems

- `Economy.Core`: market pricing now reacts to stockouts, near-term demand coverage, surplus stock, and perishability while staying clamped and deterministic.
- `GodotBridge`: `PrototypeCityView` now includes `PrototypeMarketSignal` rows with price, scarcity, market stock, warehouse stock, desired stock, consumption, and a short reason.
- `ChartersOfTrade.Godot`: city inspectors, route demand text, and priority signals now use local city pressure and shortage reasons.
- `Tests`: added coverage for stock-pressure pricing and per-city market pressure signals.
- `Project memory`: recorded the phase sync rule and the new economy state.

## Tests

- `powershell -ExecutionPolicy Bypass -File .\tools\test.ps1`: passed with 20/20 tests and `INTERACTION_SMOKE PASS`.
- `powershell -ExecutionPolicy Bypass -File .\tools\benchmark.ps1`: passed with 25/25 playable seeds, average unmet demand ratio 0.6967, median time to profit 1.0, and bankruptcy frequency 0/25 after 12 ticks.

## Notes

- `main` was fast-forwarded to `origin/main` before creating `agent/economy-depth-pass`.
- The first test run caught a ledger precision mismatch after the stronger price curve; production cash ledger entries now round to two decimals before affecting company cash.
- This is still a P0 explainability/depth layer, not final economic balance.

## Next Step

Continue with logistics/warehouse policies: reorder points, safety stock, route reservation priorities, and an overlay that makes warehouse pressure legible.
