# ADR-0008: Route Operation Network Save State

## Status

Accepted

## Context

Logistics 1.0 expands the Stage 4 single recurring charter into a small active route-operation network. The prototype now needs multiple active operations, per-route capacity contention, cargo priority, route maintenance costs, transit time, and delayed arrivals. These are gameplay state, not presentation state, because operation choices and in-transit cargo affect cash, inventories, scenario progress, and stable state hashes.

The previous `PendingRouteContractId` field could only represent one global active operation and same-tick delivery. That was enough for Stage 4, but it cannot support multiple active operations or save/load during transit.

## Decision

Move the save format to version 4.

Add `RouteOperations` to persist active recurring route operations:

- stable operation id from route, source, destination, and resource;
- source contract id for UI continuity;
- route id, endpoint direction, resource id;
- units per dispatch.

Add `RouteTransits` to persist in-flight shipments:

- stable transit id;
- source operation id;
- route id, endpoint direction, resource id;
- units, dispatched tick, arrival tick;
- expected revenue and transport cost used for deterministic arrival settlement.

Keep `PendingRouteContractId` temporarily as the selected/UI-focused contract id while route-operation state moves into the new collections.

Derived fields remain out of the save: pause reason, expected net, used/free capacity, congestion status, and UI summaries.

## Consequences

- Save/load/save hashes now include active logistics network choices and in-transit shipments.
- Multiple active operations can run across the route graph.
- Route capacity is allocated deterministically by shipment priority, expected net, and operation id.
- Road and port/coastal routes can diverge in effective capacity, maintenance cost, and transit timing without adding Godot dependencies.
- Blocking cargo through route policy can pause an operation while keeping it visible and persisted.
- Old save versions remain rejected until explicit migrations are added.
- Future fleet/unit systems should build on `RouteOperations` rather than resurrecting `PendingRouteContractId` as gameplay state.
