# Stage 1: Stabilize And Merge Readiness

## Status

Complete for planning/readiness documentation.

## Scope

- Baseline branch: `agent/mvp-roadmap-execution` at `68b3c2f`, created from verified `agent/warehouse-controls-integration`.
- Stage 1 owns PR readiness and manual QA guidance only.
- No gameplay code, simulation code, Godot scene code, or shared project memory was changed by this stage.
- Read: `PROJECT_MEMORY.md`, `AGENTS.md`, all ADRs in `docs/adr`, and `docs/agent-plans/mvp-roadmap-execution-team.md`.

## PR Readiness Checklist

- [x] Simulation core boundary preserved in the reviewed plan: Godot presentation remains outside the core per ADR-0001.
- [x] Save-state decision is recorded in ADR-0006 for warehouse modes and route policies.
- [x] Latest recorded integration verification is green: build passed; tests passed 36/36 with `INTERACTION_SMOKE PASS` and `VISUAL_SMOKE PASS`; benchmarks passed 25/25 playable seeds; visual QA passed with 15 captures.
- [x] Merge candidate includes route policy save validation and warehouse automation mode coverage per `PROJECT_MEMORY.md`.
- [ ] Re-run verification commands on the exact final merge candidate immediately before merge.
- [ ] Record one manual gameplay QA pass for the controls below.
- [ ] Confirm no unexpected working tree changes are included in the merge.
- [ ] Confirm branch head intended for merge is still synchronized with GitHub.

## Manual QA Checklist

Use several deterministic seeds, including the current default seed and at least two alternate seeds. Record seed, mode, selected city/route, and any visual or state mismatch.

### Warehouse Balanced/Conservative

- [ ] Open the prototype and confirm the Warehouse Policy panel is visible and usable at Full HD.
- [ ] In Balanced mode, confirm default policy does not show unexpected overrides and route/export behavior remains baseline.
- [ ] Switch a city/resource to Conservative mode and confirm safety stock/reorder thresholds visibly rise.
- [ ] Confirm Conservative mode affects shipment priority/exportability in the UI without creating confusing duplicate policy rows.
- [ ] Switch back to Balanced and confirm the UI returns to default behavior and does not leave stale Conservative labels.
- [ ] Save/load or use the available smoke path to confirm Conservative persists while default Balanced normalizes to no explicit saved mode.

### Route Blocking

- [ ] Select a route with multiple eligible resources.
- [ ] Block one resource from the route policy controls.
- [ ] Confirm matching route contracts disappear or become unavailable for that route.
- [ ] Confirm automatic logistics candidates no longer move the blocked resource on that route.
- [ ] Unblock the resource and confirm eligible contracts/candidates return deterministically.

### Route Priority

- [ ] Select a route with multiple reserved resources.
- [ ] Set a priority resource and confirm contract/logistics ordering favors that resource.
- [ ] Change the priority resource and confirm the displayed best/preview contract updates.
- [ ] Clear priority and confirm deterministic baseline ordering returns.
- [ ] Confirm priority cannot be set to a resource outside the route's reserved resources.

### Map Modes

- [ ] Toggle Routes, Profit, and Demand map modes.
- [ ] Confirm route lines, labels, warning marks, and supply rings remain readable in each mode.
- [ ] Confirm selected city/route and hover state remain coherent when switching modes.
- [ ] Confirm map-mode controls do not overlap the HUD, sidebar, or route labels at Full HD.

### Contract Selection

- [ ] Select a city, then inspect available route contracts.
- [ ] Select a contract and confirm summary text, selected contract id, route inspector, and expected net value update together.
- [ ] Advance ticks and confirm stale/fulfilled/unavailable contract messaging is clear.
- [ ] Change warehouse mode and route policy while a contract is selected; confirm selected/best/preview states remain understandable.
- [ ] Confirm pending route contract state survives save/load or the smoke equivalent.

## Expected Verification Commands

Run these from the repository root on the final merge candidate:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\build.ps1
powershell -ExecutionPolicy Bypass -File .\tools\test.ps1
powershell -ExecutionPolicy Bypass -File .\tools\benchmark.ps1
powershell -ExecutionPolicy Bypass -File .\tools\visual-qa.ps1
```

Optional preflight:

```powershell
git diff --check
git status --short
```

## Blockers And Risks

- Blocker before merge: manual QA for Balanced/Conservative, route blocking, route priority, map modes, and contract selection has not yet been recorded in this Stage 1 file.
- Blocker before merge: final verification should be re-run on the exact merge candidate after any later stage or coordination edits.
- Known risk: save version 2 has no v1 migration path yet; acceptable for current prototype only because ADR-0006 explicitly defers legacy migration.
- Known risk: Godot CLI and visual smoke can require non-sandboxed filesystem access for logs/rendering.
- Known risk: stale Godot debug windows can show older compiled UI; close old Godot processes before manual QA.

## Go/No-Go Recommendation

Conditional GO for merging `agent/warehouse-controls-integration` to `main`.

The branch is merge-ready from the recorded automated verification and architecture-decision perspective. Treat it as NO-GO until the manual QA checklist above is completed and the final merge candidate re-runs build, test, benchmark, and visual QA successfully. If those pass with no new P1/P0 findings, merge to `main`.
