# ADR-0007: First Charter Season Save State

## Status

Accepted

## Context

Stage 6 introduces the first explicit gameplay goal loop: `First Charter Season`. The prototype now needs deterministic objective progress, not just derived sidebar text. The goal state affects whether a run is won, timed out, or bankrupt, and must survive save/load and participate in the state hash.

The target loop is intentionally small for MVP validation:

- 12 tick season limit.
- Cash target: 1250.
- At least 3 selected route-operation deliveries.
- At least 2 distinct delivered resource ids.
- At least 4 city/resource needs stable for 3 consecutive ticks.
- Bankruptcy has precedence if company cash drops below 0 at objective evaluation.

## Decision

`SaveGame` moves to save version 3 and adds `ScenarioObjectiveSaveState`.

The state records:

- scenario id and rules version;
- started/current/end ticks;
- end reason: `in_progress`, `won`, `bankrupt`, or `timeout`;
- completed charter delivery ids;
- completed delivered resource ids;
- stable city/resource need streaks;
- latest cash and objective score.

The active rules live in `GodotBridge.FirstCharterSeason` because this is still a vertical-slice scenario coordinator, not a final reusable campaign system. The simulation core projects remain free of Godot dependencies, and `Persistence.Core` stores only neutral DTOs and validation.

## Consequences

- Scenario progress now changes save hashes.
- Save/load/save preserves objective progress deterministically.
- Old save versions are rejected until explicit migrations are added.
- The objective can be displayed consistently in Godot UI, tests, and benchmarks.
- Future full campaign/chapter systems should either promote scenario rules into a dedicated core orchestration module or add migration support before expanding save state further.
