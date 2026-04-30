# Checkpoint: MVP Stage 5 Deterministic NPC Pressure

## Summary

Implemented the first deterministic NPC pressure slice on `codex-stage5-deterministic-npc-pressure`. NPC pressure is derived from Stage 3 production-chain opportunities and Stage 4 route-operation candidates, exposed through the Godot-free bridge, and rendered in the Godot sidebar and inspectors without changing the save format.

## Changed Systems

- `AI.Company`: added pure NPC pressure candidate/score records and deterministic ranking.
- `GodotBridge`: added `PrototypeNpcPressureView`, `PrototypeSnapshot.NpcPressures`, and replaced the old raw-stock AI move source with Stage 3/4 pressure inputs.
- `ChartersOfTrade.Godot`: added NPC Pressure metric/sidebar/probe text plus city and route inspector context.
- `Tests`: added same-seed NPC pressure determinism, pressure ordering/source coverage, and simulation-core Godot dependency guards.
- `tools/visual-qa.ps1`: updated expected capture count to 18 because visual QA now captures the NPC Pressure panel for each seed.
- `docs/agent-comms`: updated Stage 5 and coordination notes.
- `PROJECT_MEMORY.md`: recorded Stage 5 decision, verification, risks, and next step.

## Tests

- `powershell -ExecutionPolicy Bypass -File .\tools\build.ps1`: passed with 0 warnings and 0 errors.
- `powershell -ExecutionPolicy Bypass -File .\tools\test.ps1`: passed with 47/47 console tests, `INTERACTION_SMOKE PASS`, and `VISUAL_SMOKE PASS`; visual smoke frame `artifacts/godot-smoke/visual-smoke-20260430-19532700000002.png`.
- `powershell -ExecutionPolicy Bypass -File .\tools\benchmark.ps1`: passed with 25/25 playable seeds, average unmet demand ratio 0.7115, median time to profit 1.0, and bankruptcy frequency 0/25.
- `powershell -ExecutionPolicy Bypass -File .\tools\visual-qa.ps1`: passed with 18 captures in `artifacts/godot-visual-qa/visual-qa-20260430-195421`.

## Review Notes

- Simulation review found a P1 where blocked/non-dispatchable route operations could still create positive NPC cash pressure. Fixed by making non-contestable candidates score zero and adding a blocked-route pressure assertion.
- Simulation review found a P2 where production pressure lost source-city context. Fixed by keeping production pressure anchored to the source city and adding separate target city context.
- Integration review returned GO with P2s for broad UI assertions and missing direct scorer fixtures. Fixed by checking the named NPC pressure log, adding an explicit NPC panel capture, and adding a direct tie/blocked scorer test.

## Risks

- NPC pressure is derived and explainable but not yet persistent NPC company state; future budgets, claims, ownership, or cooldowns need a save-format ADR.
- The benchmark corpus currently tends to rank source-production pressure highest; strategy profiles and balance tuning should come before NPC pressure becomes a durable rival commitment.
- The UI panel is informational only; no new player response action was added in this slice.

## Next Step

Merge the verified Stage 5 branch if follow-up review confirms no blockers remain.
