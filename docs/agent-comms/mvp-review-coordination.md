# MVP Roadmap Review And Coordination

## Status

Complete for this coordination pass.

## Current Notes

- Review all stage outputs against `AGENTS.md`, `PROJECT_MEMORY.md`, and ADRs.
- Track cross-stage dependencies and conflicts.
- Recommend integration order and required tests.

## Findings

- Stage 1 is a readiness/QA gate only. The warehouse controls branch remains a conditional GO, but manual Full HD QA and final verification on the exact merge candidate are still required before merging to `main`.
- Stage 2 landed as the only implementation slice in this pass. It exposes deterministic city districts and bridge-only city specializations without touching Godot runtime, save format, world generation, economy, logistics, AI, or balance.
- Stage 2 delegated review found no blockers. The one P3 concern, mutable snapshot list exposure, was fixed by returning read-only snapshot copies for city districts and specialization resource lists.
- Stage 3 should consume `PrototypeCitySpecialization.OutputResources` only as a role hint. Empty outputs on `charter_hub` and `market_exchange` must mean default/no role bonus, not "this city produces nothing."
- Stage 3 is now implemented as a read-only production-chain opportunity surface. Delegated review found and fixed the P1 protected-stock mismatch; follow-up review returned GO. Remaining P2 is that destination margin/route id are demand hints until Stage 4 route operations can realize them.
- Stage 4 is implemented with the minimal v2 save path: `PendingRouteContractId` is the single active recurring charter, while route-operation readiness, capacity use, unmet demand served, expected net, and paused reason are derived bridge/UI data. Full per-route operations remain a save v3/ADR decision.
- Stage 5 can now consume Stage 3 production opportunities and Stage 4 route-operation candidates/active-operation status before replacing the existing lightweight AI move surface.
- Stage 6 should wait for Stage 2-5 surfaces, because scenario objectives need stable city/resource needs, completed charter metrics, and optional NPC pressure summaries.

## Integration Sequence

1. Merge or keep baselined warehouse-controls integration after manual QA and final verification.
2. Keep Stage 2 city specialization as the first committed MVP roadmap slice.
3. Review and keep Stage 3 production-chain opportunities as the next committed MVP gameplay surface.
4. Keep Stage 4 route operations after delegated review.
5. Implement Stage 5 deterministic NPC pressure against Stage 3/4 candidate surfaces.
6. Implement Stage 6 "First Charter Season" objective loop after Stage 2-5 expose stable IDs and metrics.

## Verification For This Pass

- `powershell -ExecutionPolicy Bypass -File .\tools\test.ps1`: passed with 37/37 console tests, `INTERACTION_SMOKE PASS`, and `VISUAL_SMOKE PASS`.
- `powershell -ExecutionPolicy Bypass -File .\tools\benchmark.ps1`: passed with 25/25 playable seeds, average unmet demand ratio 0.7115, median time to profit 1.0, and bankruptcy frequency 0/25.
- Stage 3 verification: `tools/test.ps1` passed with 40/40 console tests plus `INTERACTION_SMOKE PASS` and `VISUAL_SMOKE PASS`; `tools/benchmark.ps1` passed 25/25 playable seeds with average unmet demand 0.7115, median time-to-profit 1.0, and bankruptcy 0/25; `tools/visual-qa.ps1` passed with 15 captures.
- Stage 4 verification after delegated review fix: `tools/test.ps1` passed with 43/43 console tests plus `INTERACTION_SMOKE PASS` and `VISUAL_SMOKE PASS`; `tools/benchmark.ps1` passed 25/25 playable seeds with average unmet demand 0.7115, median time-to-profit 1.0, and bankruptcy 0/25; `tools/visual-qa.ps1` passed with 15 captures in `artifacts/godot-visual-qa/visual-qa-20260430-162456`.
- Stage 4 delegated review returned no blockers. The non-blocking dispatch test gap was fixed with a deterministic wood-delivery fixture that asserts real `route operation delivered` ledger output, positive route cash, and tick ledger/cash consistency.

## Final Recommendation

Proceed to delegated review for Stage 4, then Stage 5 deterministic NPC pressure. Keep full multi-route operations deferred until a save v3/ADR decision.
