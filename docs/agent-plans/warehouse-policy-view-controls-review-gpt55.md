# GPT-5.5 Review: Warehouse Policy View Controls

## Scope

Read-only review of the Godot runtime/UI diff for warehouse policy view controls.

## Touched Systems

- `ChartersOfTrade.Godot`: `BootstrapPanel.cs`
- `ChartersOfTrade.Godot`: `InteractionSmokeRunner.cs`
- `docs/agent-plans`: team coordination plan

## Findings

- P3: The policy focus dropdown initially used `Auto priority` even when Safety or Reorder view modes were active. Fixed by relabeling the auto option per mode: `Auto priority`, `Auto safety`, or `Auto reorder`.
- P3: Interaction smoke covered city-click focus and policy mode buttons, but did not exercise the new policy focus dropdown path directly. Fixed by giving the policy and contract dropdowns stable names and selecting a focus city through `PolicyFocusOptions`.

## Fixed/Deferred

- Fixed both P3 findings.
- Deferred full Godot smoke execution in this sandbox because Godot cannot open `user://logs` and crashes before the smoke scene can report `INTERACTION_SMOKE PASS`.

## Tests Required

- `tools/build.ps1`
- Console test runner, or full `tools/test.ps1` where Godot user log writes are available
- `tools/benchmark.ps1`
- Full interaction and visual smoke outside the current sandbox

## Stop Conditions

- Do not let warehouse policy view state enter `GodotBridge`, simulation core, save DTOs, or state hashes unless a save-format decision is recorded.
- Do not treat these controls as gameplay automation; they are inspection aids over existing `PrototypeMarketSignal` data.

## Next Step

Run full Godot interaction and visual smoke outside the sandbox to verify the updated dropdown workflow and visual fit.

