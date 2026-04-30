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
- Stage 4 should choose its save path before implementation. Reusing the current selected contract is the smallest path; full route operations need a save-version/ADR decision.
- Stage 5 should wait for Stage 3 production opportunities and Stage 4 route-operation candidates before replacing the existing lightweight AI move surface.
- Stage 6 should wait for Stage 2-5 surfaces, because scenario objectives need stable city/resource needs, completed charter metrics, and optional NPC pressure summaries.

## Integration Sequence

1. Merge or keep baselined warehouse-controls integration after manual QA and final verification.
2. Keep Stage 2 city specialization as the first committed MVP roadmap slice.
3. Implement Stage 3 as a read-only production-chain opportunity surface and compact UI section.
4. Implement Stage 4 route operations after choosing the save path and recording any required ADR.
5. Implement Stage 5 deterministic NPC pressure against Stage 3/4 candidate surfaces.
6. Implement Stage 6 "First Charter Season" objective loop after Stage 2-5 expose stable IDs and metrics.

## Verification For This Pass

- `powershell -ExecutionPolicy Bypass -File .\tools\test.ps1`: passed with 37/37 console tests, `INTERACTION_SMOKE PASS`, and `VISUAL_SMOKE PASS`.
- `powershell -ExecutionPolicy Bypass -File .\tools\benchmark.ps1`: passed with 25/25 playable seeds, average unmet demand ratio 0.7115, median time to profit 1.0, and bankruptcy frequency 0/25.

## Final Recommendation

Proceed with Stage 3 next. Keep it read-only first, because that gives Stage 5 and Stage 6 stable planning data without forcing a save-format decision too early.
