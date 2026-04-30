# ADR-0006: Warehouse Policy Is Gameplay State

## Status

Accepted

## Context

Warehouse reorder thresholds, reserve stock, and route goods priorities now change logistics outcomes. They are no longer only Godot diagnostics or inspector text.

## Decision

Warehouse and route policies are deterministic simulation state exposed through `GodotBridge` snapshot views and changed through explicit `PrototypeSession` methods. Godot controls may present and invoke these methods, but UI selection, hover, layout, and styling remain presentation-only.

The prototype save format is bumped to version 2 and includes warehouse policies and route policies in the state hash. Invalid policy changes return `false` and leave the current snapshot/hash unchanged.

## Consequences

- Policy controls can be tested through console determinism tests and Godot interaction smoke.
- Route contracts, automatic logistics, and inspector summaries read the same policy state.
- Future save migrations must account for policy fields before supporting durable external saves.
