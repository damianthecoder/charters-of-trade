# Stage 6: MVP Player Goal Loop

## Status

Planning complete for this pass. No gameplay code changed.

## Current Notes

- Start from `agent/mvp-roadmap-execution`.
- Define objective state carefully before implementation.
- Save/hash implications require coordination and review.
- Stage 6 should not implement until Stages 2-5 expose stable city role, production chain, route operation, and NPC pressure surfaces.

## Findings

### MVP Scenario Loop: "First Charter Season"

Target play length: 10-15 minutes.

Simulation shape: a 12-week starter scenario using the existing deterministic tick model. The player can pause, inspect, and advance ticks; the scenario is balanced around making 3-5 meaningful decisions before the end summary rather than around long-run optimization.

Player-facing loop:

1. Week 0 orientation: inspect city specializations, current market pressure, starting warehouse policy, route policies, and the initial charter contract board.
2. Weeks 1-3 stabilization: pick one anchor supply problem, set warehouse mode/thresholds, reserve or block cargo on one route, and accept the first charter that matches a city need.
3. Weeks 4-7 production and routing: commit to one visible production-chain opportunity, keep its inputs stocked, and assign recurring route operations so the chain has a reliable flow.
4. Weeks 8-10 pressure response: react to an NPC company move or market shortage by changing route priority, contract choice, or warehouse policy.
5. Weeks 11-12 closeout: decide whether to chase final cash, finish a charter, or protect supply stability before the scenario ends.

P0 scope guard: this is a scenario objective wrapper over the existing graph-first economy/logistics prototype. It is not a campaign layer, story system, achievement system, territory-control victory, or dynamic event framework.

### Objective Shape

Recommended starter win condition after 12 weekly ticks:

- Cash target: finish with at least `1,250` company cash and never trigger bankruptcy.
- Charter execution: complete at least `3` charter deliveries, with at least `2` different resource ids represented.
- Supply stability: keep at least `4` tracked city/resource needs stable for the final `3` ticks. Stable means the destination's current stock is at or above its effective reorder point, using warehouse policy overrides/modes when present.
- Bankruptcy guard: fail immediately if company cash is below `0` at the end-of-tick objective evaluation step.

End-state precedence:

1. Bankruptcy failure.
2. Win if all required objectives are complete.
3. Timeout loss if tick limit is reached without all required objectives.
4. In progress otherwise.

The concrete values above are intended as first-pass balance targets. They should be validated against the benchmark seeds after Stages 2-5 land; if they are too strict or too soft, adjust the scenario content/spec before implementation rather than hardcoding balance constants.

### Objective State Contract

Stage 6 should introduce Godot-free scenario state only after the required upstream surfaces are stable.

Candidate immutable spec:

- `ScenarioId`
- `ScenarioRulesVersion`
- `TickLimit`
- `CashTarget`
- `BankruptcyFloor`
- `RequiredCompletedCharters`
- `RequiredDistinctCharterResources`
- `RequiredStableNeedCount`
- `StabilityWindowTicks`
- Score weights, if score is data-driven rather than code constants.

Candidate mutable progress:

- `ScenarioId`
- `ScenarioRulesVersion`
- `StartedTick`
- `CurrentObjectiveTick`
- `EndTick`
- `EndReason` (`inProgress`, `won`, `bankrupt`, `timeout`)
- `CompletedCharterIds`
- `CompletedCharterResourceIds`
- `StableNeedStreaks` keyed by canonical `cityId/resourceId`
- `FinalCash`
- `FinalScore`
- Optional deterministic summary ids for important NPC pressure and player route-operation milestones, if Stage 5 exposes them cleanly.

State rules:

- Count a charter exactly once, when the route operation/contract ledger reports deterministic completion.
- Evaluate objective progress after economy, production, logistics, AI, and persistence-facing tick state have settled for the tick.
- Use canonical ids only. Do not store display labels, dropdown indices, transient contract rankings, or Godot node state.
- Objective state belongs in the core/bridge/persistence boundary, not in `ChartersOfTrade.Godot`.

### Score And End Summary

The end summary should explain why the run ended and what the player controlled. Recommended score out of 100:

- 35 Economy: final cash progress toward `CashTarget`, capped at full credit.
- 25 Charter execution: completed charters and distinct delivered resources.
- 25 Supply stability: final-window stable city/resource needs.
- 15 Resilience: no bankruptcy warning ticks, fewer emergency stockouts, and positive end cash buffer.

Summary rows:

- Result: won, bankrupt, or timeout.
- Final cash and cash target.
- Completed charters, required charters, and delivered resource variety.
- Stable needs in the final window.
- Best route operation by delivered value or demand served, once Stage 4 exposes that metric.
- Most important NPC pressure or contested opportunity, once Stage 5 exposes deterministic AI summaries.
- One next-step hint derived from the weakest score bucket.

The summary can be recomputed from objective progress at load time. If a cached summary is persisted for UX, it must be canonical and hash-equivalent to recomputation.

### Save And Hash Implications

- Scenario progress is gameplay state. It should be included in save/load and the stable state hash.
- Adding scenario progress is a save-format change. Coordinate an ADR/checkpoint update and likely move beyond save version `2` rather than silently extending the warehouse/route policy save shape.
- Scenario spec data should participate in compatibility checks. If scenarios are JSON content, changes to objective targets should affect `contentHash`; if they are code-defined for P0, persist `ScenarioRulesVersion` so old saves can reject incompatible rules.
- `CompletedCharterIds`, stable-need keys, and any milestone ids must be sorted canonically before hashing.
- Do not include UI-only objective tracker state in the save hash.
- Do not make the score depend on current-culture number formatting, wall-clock time, frame count, or Godot presentation state.
- Save/load tests should prove objective state does not double-count completed charters after reload.

### Tests Needed

Core/bridge tests:

- Same seed, same scenario spec, and same scripted decisions produce the same objective hash and end summary.
- Save-load-save preserves objective progress, stable need streaks, completed charter ids, end reason, and final score.
- A completed charter is counted once across repeated snapshots and across reload.
- Bankruptcy end-state precedence is deterministic.
- Timeout occurs at the configured tick limit when objectives are incomplete.
- Win occurs when cash, charter, distinct-resource, and stability requirements are all satisfied.
- Objective scoring uses invariant/canonical numeric formatting.

Integration tests after Godot UI work:

- Interaction smoke can see the objective tracker, advance to an ended run or a forced near-end fixture, and read the result summary.
- Visual QA includes at least one objective tracker frame and one end summary frame.
- Benchmark output reports scenario win rate, average score, and common failure reason without replacing existing playability KPIs.

### Integration Dependencies

- Stage 2, City Specialization: needs stable city roles and the canonical set of city/resource needs that can count toward supply stability.
- Stage 3, Production Chains: needs a player-readable production-chain opportunity and, ideally, a deterministic margin/explanation surface for the end summary.
- Stage 4, Route Operations: needs recurring route operations, completion/delivery ledgers, and stable ids for completed charters or route jobs.
- Stage 5, NPC Company AI: needs deterministic NPC move summaries before NPC pressure appears in scoring. If Stage 5 is not ready, NPC pressure should be summary-only or omitted from the first win condition.
- Persistence coordination: adding objective progress needs save/hash review and probably a save version bump/ADR.
- Godot coordination: UI should only render scenario snapshots and issue existing gameplay commands; objective evaluation must stay outside Godot.

### Suggested Implementation Order

1. Wait for Stage 2-5 comm files or patches to define the upstream data surfaces.
2. Add a small Godot-free `ScenarioObjective` model/spec/progress layer in the bridge or a simulation-adjacent project, depending on where the final route operation ledger lands.
3. Persist scenario progress and update stable hashing with canonical ordering.
4. Add deterministic unit tests before any Godot UI.
5. Add a compact objective tracker and end summary to the Godot prototype.
6. Extend benchmark output with scenario result metrics.
