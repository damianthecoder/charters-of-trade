# ADR-0006: Warehouse Policy Save State

## Status

Accepted

## Decision

Persist player warehouse policy overrides as explicit save state in `SaveGame.WarehousePolicies` and move the prototype save version to `2`.

## Context

Warehouse controls are now gameplay state: changing safety stock or reorder point affects shipment priority, route contract availability, exportable warehouse stock, and the deterministic state hash. The policy must survive save/load and must be testable outside Godot.

## Consequences

- Save validation now requires the current save version instead of accepting any positive version.
- Policy overrides are sorted before hashing so equivalent saves hash identically.
- The first implementation only accepts resources with tracked market needs; non-need resources cannot create hash-only no-op policies.
- Future save migration work should add an explicit v1-to-v2 migration path before loading legacy saves.
