# MVP Stage 4 Route Operations

## Summary

Implemented the first recurring route-operation slice on
`agent/mvp-roadmap-execution` using the minimal save v2 path. A selected route
contract now becomes one active recurring charter stored through
`PendingRouteContractId`; operation readiness, capacity use, unmet demand served,
expected net, and paused reason are derived bridge/UI data.

## Changed Systems

- `GodotBridge`: added `PrototypeRouteOperationView`,
  `RouteOperationCandidates`, `ActiveRouteOperation`, active operation
  execution, pause reasons, and `ClearRouteOperation`.
- Logistics prototype loop: selected operations now take the route decision
  before automatic logistics and respect route policy, warehouse safety stock,
  exportable source stock, destination demand gap, capacity cap, and expected
  net.
- Godot UI: Route Contract panel now explains route-operation status, used/free
  capacity, unmet demand served, expected net, and stop control; route inspector
  and system probe report active operations.
- Tests: added route-operation ordering, active state, blocked-cargo pause, UI
  smoke, and visual QA coverage.
- Documentation: updated project memory plus Stage 4 and review-coordination team
  notes.

## Tests

- `powershell -ExecutionPolicy Bypass -File .\tools\test.ps1`: passed with 43/43
  console tests, `INTERACTION_SMOKE PASS`, and `VISUAL_SMOKE PASS`; visual smoke
  frame `artifacts/godot-smoke/visual-smoke-20260430-16243500000002.png`.
- `powershell -ExecutionPolicy Bypass -File .\tools\benchmark.ps1`: passed with
  25/25 playable seeds, average unmet demand ratio 0.7115, median time to profit
  1.0, bankruptcy frequency 0/25.
- `powershell -ExecutionPolicy Bypass -File .\tools\visual-qa.ps1`: passed with
  15 captures in `artifacts/godot-visual-qa/visual-qa-20260430-162456`.

## Review Notes

- Delegated simulation and integration reviewers found no blockers.
- Non-blocking dispatch coverage gap was fixed with a deterministic wood-delivery
  fixture asserting actual `route operation delivered` ledger output, positive
  route cash, and tick ledger/cash consistency.

## Risks

- Minimal save v2 supports only one active recurring operation globally. Full
  one-operation-per-route behavior, unit caps, and in-transit lead times remain
  deferred until a save v3/ADR decision.
- Route operation candidates still derive from charter-facing route contracts;
  broader source/destination route operations should be handled as a separate
  gameplay slice.

## Next Step

Run delegated review and then proceed to Stage 5 deterministic NPC pressure.
