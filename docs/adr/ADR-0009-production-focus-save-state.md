# ADR-0009: Production Focus Save State

## Status

Accepted

## Context

Stage 3 production-chain opportunities were previously read-only bridge/UI data. The player could see useful production chains, bottlenecks, destination demand, and margins, but production itself still ran automatically in deterministic recipe order.

The next company-operations layer needs production to become a player commitment without turning the MVP into a parcel builder or a factory micromanagement game. A city-level policy is the smallest durable state that lets the player say: keep this city automatic, prioritize this recipe, or pause production while warehouses and routes recover.

## Decision

Move the save format to version 5.

Add `ProductionPolicies` to `SaveGame` as city-scoped gameplay state:

- `cityId`: saved city affected by the policy;
- `focusRecipeId`: recipe to prioritize, present only in focus mode;
- `mode`: `auto`, `focus`, or `paused`.

`auto` is the default and normalizes out of state hashes when no focus recipe is present. `focus` requires a recipe id and affects the state hash. `paused` stores no recipe id and affects the state hash.

The prototype bridge exposes `PrototypeProductionPolicyView` plus `SetProductionFocus`, `ClearProductionFocus`, and `PauseProduction`. Focus mode runs the focused recipe before lower-priority recipes and reserves focused inputs from lower-priority recipes if the focused recipe cannot run yet. Paused mode skips production for that city.

## Consequences

- Production focus is gameplay state, not presentation state.
- Save/load/save hashes now include non-default production policies.
- Old save versions remain rejected until explicit migrations are added.
- Default auto behavior remains close to the previous automatic production loop and stays compact in canonical hashes.
- UI controls can set a city focus without adding Godot dependencies to the simulation core.
- Future contract-board and scenario-strategy work can use production policy as the player-authored production intent instead of inferring intent from read-only opportunities.
