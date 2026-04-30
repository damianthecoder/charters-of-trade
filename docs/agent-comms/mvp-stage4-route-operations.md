# Stage 4: Route Operations

## Status

Planning pass complete. No gameplay code changed in this pass because route save
state is active and shared.

## Context Read

- Read `PROJECT_MEMORY.md`, `AGENTS.md`, all current ADRs, and
  `docs/agent-plans/mvp-roadmap-execution-team.md`.
- Adjacent stage notes for Stage 3 and Stage 6 are still pending, so Stage 4
  should expose clean deterministic surfaces without assuming their final UI or
  scoring choices.
- Relevant ADR constraints: keep simulation logic Godot-free, use deterministic
  discrete ticks, keep economy/logistics on the node graph, and treat player
  route policy choices as save/hash-affecting gameplay state.

## Findings

### Existing Route State

- `PrototypeRouteContractView` already exposes the right public shape for a
  charter candidate: route, direction, cargo, expected revenue, transport cost,
  expected net, capacity, units, shipment priority, and policy reason.
- `PendingRouteContractId` is already persisted in save v2. The current selected
  contract behaves like a recurring charter while it remains available, but the
  model is still named as a pending one-off route contract.
- `RoutePolicySaveState` already persists route-level allowed cargo and optional
  priority cargo. For Stage 4, blocked cargo should mean "not present in the
  route policy reserved resource list"; do not add a second blocked-cargo model.
- Current logistics settles deliveries in the same tick. `TradeRoute.LeadDays`
  exists for route metadata, but there is no persisted in-transit shipment queue.

### Recurring Route Operation Design

Formalize a route operation as an active recurring charter:

- A route operation is "move this resource from this source city to this
  destination city on this route every eligible tick until paused, blocked, or
  replaced."
- MVP scope should allow at most one active recurring operation per route. That
  gives the player meaningful route ownership without creating a fleet scheduler
  or multi-leg logistics game.
- The stable operation id should be deterministic from route id, direction, and
  resource, for example `route-003:n4->n0:grain`. Do not use display names.
- Priority cargo remains the route policy priority resource. An active operation
  should win capacity on its route before automatic leftover logistics, but it
  must still respect warehouse safety stock, route resource reservations, and
  current exportability.
- Blocked cargo is enforced by the existing route policy resource list. If the
  operation cargo is removed from that list, the operation becomes inactive or
  invalid rather than secretly bypassing the block.
- Per-tick units should be deterministic:
  `min(route capacity, operation unit cap, exportable source warehouse stock,
  destination demand gap)`. If the first implementation does not expose a unit
  cap, treat the operation cap as route capacity.
- Expected profit should initially use the existing contract math:
  destination price times expected units minus route transport cost times units.
  Stage 3 can later provide true chain margin or input cost attribution.
- Unmet demand served should be reported as
  `min(units delivered, max(0, desired stock - destination market stock before delivery))`.
- If no units can move, produce a deterministic paused reason such as
  `blocked cargo`, `no exportable stock`, `destination stocked`, or
  `negative expected net`. These reasons are UI/runtime output, not saved state.

For the first safe implementation slice, keep same-tick settlement. If lead-day
arrival is introduced, add a separate persisted in-transit shipment queue and an
ADR before changing code.

### Save Implications

There are two safe paths:

- Minimal v2 path: keep using `PendingRouteContractId` as the single active
  charter id and rename/explain it in bridge/UI surfaces only. This avoids a save
  format change but only supports one active recurring charter across the map.
- Full route-operations path: add a `RouteOperations` collection to the save
  model, advance the save version, and write an ADR/checkpoint before
  implementation.

Recommended full save shape:

```csharp
public sealed record RouteOperationSaveState(
    string Id,
    string RouteId,
    string FromNode,
    string ToNode,
    string ResourceId,
    bool IsActive,
    int UnitsPerDispatch);
```

Save validation should require:

- operation id is non-empty and deterministic from route, direction, and cargo;
- referenced route exists;
- from/to are the two endpoints of the route;
- resource id is listed in the saved route `reservedFor` list;
- resource id is currently allowed by that route's `RoutePolicySaveState`;
- one active operation per route for MVP;
- `UnitsPerDispatch` is positive when present, or normalized to route capacity if
  the design uses an auto-fill sentinel;
- operations are sorted by id before hashing;
- expected profit, last run result, demand served, and paused reason are derived
  snapshot data and are not saved.

Do not add in-transit lead-time behavior without also saving enough shipment
state to survive save/load between dispatch and arrival.

### Test Expectations

Required deterministic tests before Stage 4 implementation is considered ready:

- Route operation candidates have stable ordering by priority, expected net, then
  id.
- Selecting or changing an active recurring operation changes the stable state
  hash when the chosen save path makes it gameplay state.
- Save-load-save preserves active route operations and produces the same hash.
- Route operations reject unknown routes, invalid directions, unknown resources,
  resources outside `reservedFor`, and resources blocked by route policy.
- Blocking cargo through route policy pauses or removes the affected operation
  deterministically.
- Priority cargo receives capacity before lower-priority automatic logistics on
  the same route.
- Capacity is capped per route per tick, and leftover capacity can still run the
  existing automatic logistics path.
- Expected net uses the same rounding path as existing route contracts.
- Unmet demand served is computed from destination state before delivery.
- Same seed plus same operation choices produces the same ledger and save hash
  across two sessions.
- If UI changes are included, run `tools/build.ps1`, `tools/test.ps1`,
  `tools/benchmark.ps1`, and `tools/visual-qa.ps1`.

### UI Expectations

The route inspector should make recurring operations legible without inventing a
new game mode:

- Show active charter cargo, source, destination, route capacity, expected units,
  expected net, unmet demand served, and paused reason.
- Let the player activate a charter from an available route contract and stop or
  replace the active charter for that route.
- Reuse existing route policy controls for priority and blocked cargo.
- Show capacity as used/free for the selected route after the active operation
  takes its share.
- Show lead days as route information only until in-transit shipments are saved.
- Keep display-only UI state out of the save file; active operations and policy
  changes are gameplay state.

### Dependencies

- Stage 3 production chains should supply chain opportunity/margin explanation
  when available. Until then, Stage 4 should use existing destination-price minus
  transport-cost contract math and avoid inventing production cost attribution.
- Stage 4 should expose deterministic operation summaries that Stage 3 can link
  to source production opportunities: route id, cargo, source, destination,
  expected units, expected net, and blocked/paused reason.
- Stage 6 goal loop needs stable counters for charter activity, supply stability,
  cash contribution, and unmet demand served. Stage 4 should expose those as
  derived snapshot fields rather than Stage 6 scraping ledger text.
- If Stage 6 decides active charter count or contract completion is objective
  progress, coordinate the save format so objective state and route operation
  state do not duplicate each other.

### Safe Implementation Order

1. Wait for Stage 3 and Stage 6 notes or coordinator guidance if they land before
   implementation starts.
2. Choose the save path explicitly. If moving beyond `PendingRouteContractId`,
   add an ADR and save-version plan before touching code.
3. Add Godot-free route operation view/result records in the bridge or a core
   logistics service, derived from existing route contracts and policies.
4. Add deterministic tests for candidate ordering, capacity use, blocking,
   priority, expected net, unmet demand served, and save/hash behavior.
5. Wire the route inspector to the new operation summary while reusing existing
   route policy controls.
6. Run build, tests, benchmarks, and visual QA. Because this touches logistics,
   save/load, and Godot UI, request delegated review before final integration.
