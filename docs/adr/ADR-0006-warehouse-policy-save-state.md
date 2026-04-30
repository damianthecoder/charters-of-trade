# ADR-0006: Warehouse And Route Policy Save State

## Status

Accepted

## Decision

Persist player warehouse policy overrides as explicit save state in `SaveGame.WarehousePolicies` and move the prototype save version to `2`.

Treat warehouse automation mode as part of warehouse policy state only when it is non-default. Balanced mode is the default and normalizes to no saved `mode`; Conservative mode is saved as `mode: conservative`.

Persist route-level resource reservations and optional priority resources as explicit save state in `SaveGame.RoutePolicies`.

Require one route policy entry for every saved route. Route policy resource ids must be listed in that route's saved `reservedFor` resources, and starter/default route resources are derived from declared market needs.

## Context

Warehouse controls are now gameplay state: changing safety stock or reorder point affects shipment priority, route contract availability, exportable warehouse stock, and the deterministic state hash. The policy must survive save/load and must be testable outside Godot.

Warehouse automation mode changes the same underlying thresholds. Conservative mode raises safety/reorder thresholds and therefore affects priority, exportability, contracts, logistics, and state hashes. Balanced remains the default policy so existing default saves do not carry redundant mode fields.

Route controls are also gameplay state: blocking a resource on a route removes matching route contracts and automatic logistics candidates for that route, while setting a route priority resource changes deterministic contract/logistics ordering. These controls must survive save/load and remain Godot-free.

## Consequences

- Save validation now requires the current save version instead of accepting any positive version.
- Policy overrides are sorted before hashing so equivalent saves hash identically.
- Warehouse policy mode validation only accepts `balanced` or `conservative`; `balanced` normalizes to null for canonical hashing.
- Conservative policy mode is a P0 automation preset over the same safety/reorder mechanics, not a separate logistics system.
- The first implementation only accepts resources with tracked market needs; non-need resources cannot create hash-only no-op policies.
- Route policies are sorted before hashing; reserved resources are normalized inside each policy.
- A route priority resource must be one of the route's reserved resources.
- Saved route policies must cover every saved route so absent policy state cannot silently mean a different logistics model.
- Route policy resources are validated against saved route `reservedFor` resources so no hash-only or UI-only resource ids can affect determinism.
- Future save migration work should add an explicit v1-to-v2 migration path before loading legacy saves.
