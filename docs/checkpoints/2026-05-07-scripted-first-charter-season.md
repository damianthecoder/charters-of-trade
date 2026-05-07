# Checkpoint: Scripted First Charter Season

Date: 2026-05-07

## Summary

Implemented Slice 3 from the company-operations plan: a deterministic scripted First Charter Season benchmark path that uses production focus, route priority, a small active route-operation network, and active-operation selection to pursue charter deliveries. The benchmark now reports naive and scripted scenario outcomes separately.

## Changed Systems

- `GodotBridge`: added `FirstCharterSeasonScriptedStrategy` and result DTO.
- `PrototypeSession`: active route operations can now be selected directly by operation id or source contract id; active route-operation cargo reserves production output before local market sale.
- `Benchmarks`: CSV and summaries now include `scenario_win_tick`, `total_charter_deliveries`, `distinct_delivered_resources`, `stable_need_ticks`, and separate scripted scenario/throughput/focus metrics.
- `Tests`: added scripted season coverage proving at least one benchmark seed can win deterministically.
- `docs/agent-plans/next-feature-company-operations-plan.md` and `PROJECT_MEMORY.md`: updated Slice 3 status and next-step guidance.

## Tests

- `git diff --check`: passed.
- `powershell -ExecutionPolicy Bypass -File .\tools\build.ps1`: passed with 0 warnings and 0 errors.
- `powershell -ExecutionPolicy Bypass -File .\tools\test.ps1`: passed with 65/65 tests, `INTERACTION_SMOKE PASS`, and `VISUAL_SMOKE PASS`; latest visual smoke frame: `artifacts/godot-smoke/visual-smoke-20260507-06273400000002.png`.
- `powershell -ExecutionPolicy Bypass -File .\tools\benchmark.ps1`: passed with 25/25 playable seeds. Naive scenario wins/timeouts/bankruptcies: 0/25/0. Scripted scenario wins/timeouts/bankruptcies: 6/19/0. Median scripted win tick: 8.0. Average scripted scenario score: 85.0.

## Review Notes

- Fixed: the first scripted attempt only reached two selected deliveries because it ran one operation at a time; the strategy now keeps up to three active operations and selects the arriving operation before the tick resolves.
- Fixed: focused production output was being sold locally before logistics could use it; route cargo reservations now protect output above warehouse safety stock.
- Deferred: no delegated reviewer was spawned because the current tool policy allows subagents only when the user explicitly requests delegation. Main-agent review plus full test/benchmark verification was performed instead.

## Risks

- Scripted win rate is intentionally recorded as balance data, not tuned to 100%. Remaining failures mostly miss distinct-resource or timely third-delivery requirements despite good cash and stable needs.
- The scripted strategy is benchmark tooling, not final AI or tutorial behavior.
- Production-to-logistics reservation is still coarse and should be revisited when accepted contracts and explicit deadlines exist.

## Next Step

Implement Contract Board V0 with deterministic offers, accepted-contract progress, deadlines/rewards, and visible route/resource bottlenecks.
