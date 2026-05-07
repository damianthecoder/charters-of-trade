# Next Feature Plan: Company Operations Loop

## Status

Slices 1-3 implemented on 2026-05-07. Slice 4, Contract Board V0, is the recommended next implementation step.

## Research Summary

The current prototype already validates the graph-first thesis: generated trade graph, local markets, warehouse policies, route policies, route operations, NPC pressure, and a 12-tick scenario objective all work deterministically. The next feature should not expand into warfare, parcel city building, or a larger commodity tree yet. It should turn existing read-only signals into player-owned company operations.

External references point in the same direction:

- [Slipways on Steam](https://store.steampowered.com/app/1264280/Slipways/) emphasizes trade connections, short runs, low micromanagement, and immediate consequences. The useful lesson is to make each network decision legible and impactful, not to simulate every carrier unit.
- [Offworld Trading Company gameplay](https://www.offworldgame.com/game/gameplay) frames money and the market as the battlefield, with supply/demand, extraction, imports, refinement, and business models as the core choices. The useful lesson is that economic conflict can be clear without direct combat.
- [Grand Ages: Medieval on Steam](https://store.steampowered.com/app/310470/Grand_Ages_Medieval/) supports the medieval trade fantasy: city founding, production, trade goods, research, expansion, and routes. The useful lesson is to give cities understandable production identities and trade purposes.
- [Victoria 3 Dev Diary #37](https://www.paradoxinteractive.com/games/victoria-3/news/dev-diary-37-market-expansion) highlights infrastructure, ports, market access, and transport costs as separate constraints from price. The useful lesson is that route capacity and access should become first-class decisions before broader diplomacy or market-union mechanics.
- The local concept PDF, `Projekt i plan wdrożenia hybrydowej gry ekonomiczno-strategicznej inspirowanej Slipways i Grand Ages - Charters of Trade.pdf`, argues for a graph-first mercantile city builder where company value comes from controlling flows, not war or house-by-house building.

## Current Gap

The prototype shows good opportunities but does not yet give the player enough operational authorship:

- Production chain opportunities are visible and deterministic, but production itself is mostly automatic.
- City specialization is visible, but city development is not yet a player commitment.
- Route operations are persisted and controllable, but benchmark throughput is still measured mostly by end-state counts rather than total dispatched/arrived volume.
- NPC pressure is explainable, but rival action is still derived pressure, not a lasting competitor commitment.
- The First Charter Season objective can be won by rules tests, but unattended benchmark rows still time out, so the benchmark is not yet a strong balance signal.

## Recommendation

Implement **Production Focus + Flow Throughput**, not new goods or fleets.

This is the smallest next step that makes the game feel more like a company-management game:

1. The player chooses what a city should prioritize producing.
2. Warehouse safety stock and route operations constrain that choice.
3. Logistics must move the focused output to demand.
4. Scenario scoring and benchmarks can measure whether the network is actually working.
5. NPC pressure can react to the player's production focus without needing full rival inventory yet.

## Implementation Order

### Slice 0: Repo And Branch Hygiene

Goal: start clean before touching save/load or economy behavior.

- Commit or otherwise resolve the current overlay clarity changes.
- Create a focused branch from current `main`, suggested name: `codex/company-operations-loop`.
- Re-run `tools/test.ps1` once on the branch before behavior changes.

Acceptance:

- Working tree is clean except intended branch work.
- Baseline test result is recorded in the next checkpoint.

### Slice 1: Flow Throughput Metrics

Status: Implemented on 2026-05-07.

Goal: make logistics balance measurable before changing production behavior.

Files likely touched:

- `src/GodotBridge/PrototypeSession.cs`
- `benchmarks/ChartersOfTrade.Benchmarks/Program.cs`
- `tests/ChartersOfTrade.Tests/Program.cs`

Work:

- Add derived snapshot metrics for route-operation throughput this run: total dispatches, total arrivals, total units dispatched, total units arrived, and total unmet demand served.
- Keep these metrics out of save state at first; they are benchmark/UI telemetry unless we later need historical graphs.
- Update benchmark CSV and summary with total arrived units and total unmet demand served.
- Add tests proving deterministic throughput metrics across same seed and same route-operation selections.

Acceptance:

- Tests cover same-seed deterministic throughput.
- Benchmark reports total route throughput, not only final active operations and final in-transit shipments.
- No save version change.

### Slice 2: Production Focus Save State

Status: Implemented on 2026-05-07.

Goal: make production a player decision while preserving deterministic save/load.

Architecture decision:

- Add `ADR-0009-production-focus-save-state.md`.
- Move save format to version 5 unless an explicit migration layer is added.

Suggested save DTO:

```csharp
public sealed record ProductionPolicySaveState(
    string CityId,
    string? FocusRecipeId,
    string Mode);
```

Suggested modes:

- `auto`: default, normalized to no saved policy where possible.
- `focus`: prioritize one recipe in this city.
- `paused`: do not run production recipes in this city.

Files likely touched:

- `src/Persistence.Core/SaveGame.cs`
- `src/GodotBridge/PrototypeSession.cs`
- `tests/ChartersOfTrade.Tests/Program.cs`
- `src/ChartersOfTrade.Godot/Scripts/BootstrapPanel.cs`
- `docs/adr/ADR-0009-production-focus-save-state.md`

Work:

- Add production policy persistence, canonical sorting, validation, and hash participation.
- Add `PrototypeProductionPolicyView` and bridge methods such as `SetProductionFocus(cityId, recipeId)`, `ClearProductionFocus(cityId)`, and `PauseProduction(cityId)`.
- Change `RunProduction` so focus mode runs the focused recipe first and protects its inputs from lower-priority recipes.
- Keep default `auto` behavior close to current behavior for compatibility with existing tests.
- Expose focus state in city inspector, Production Chains panel, map badge text, and Stage 3-6 status.

Acceptance:

- Setting production focus changes save hash immediately.
- Save-load-save preserves production policy and hash.
- Invalid city/recipe/mode inputs are rejected without tick or hash changes.
- Focused production runs before non-focused recipes and respects warehouse safety stock.
- Godot smoke can set one focus from the UI and observe updated status text.

### Slice 3: First Charter Season Scripted Win Path

Status: Implemented on 2026-05-07. The scripted benchmark strategy wins 6/25 benchmark seeds while naive play remains 0/25, so the remaining timeouts are recorded as balance data rather than hidden tuning.

Goal: make scenario balance measurable without pretending the benchmark is a full player.

Files likely touched:

- `benchmarks/ChartersOfTrade.Benchmarks/Program.cs`
- `tests/ChartersOfTrade.Tests/Program.cs`
- optionally `src/GodotBridge/FirstCharterSeason.cs`

Work:

- Add a scripted benchmark strategy that sets production focus and selects route operations aimed at the three charter requirements.
- Add benchmark rows for `scenario_win_tick`, `total_charter_deliveries`, `distinct_delivered_resources`, and `stable_need_ticks`.
- Keep the existing naive route-operation benchmark columns so regressions remain comparable.

Acceptance:

- At least one scripted seed can win First Charter Season in tests.
- Benchmark reports win rate for scripted route/production play separately from naive auto-play.
- If win rate remains low, record the failure as balance data rather than silently tuning rules.

### Slice 4: Contract Board V0

Goal: convert the season objective into visible short-term offers.

Architecture decision:

- If contracts are accepted/declined and survive save/load, add a save-state ADR or extend ADR-0009.

Recommended minimal version:

- Generate deterministic contract offers from current city needs and route feasibility.
- Persist only accepted contracts, not every generated offer.
- Accepted contract fields: contract id, source/destination, resource, required units, delivered units, deadline tick, reward, penalty, state.

Acceptance:

- Contract acceptance affects save hash.
- Route deliveries credit accepted contracts with structured dispatch results, not ledger text.
- UI lists 3-5 offers with reward, deadline, route hint, and bottleneck.
- First Charter Season can reference accepted contract progress instead of only generic selected-operation deliveries.

### Slice 5: Rival Claim Preview

Goal: make NPC pressure feel like competition without full rival economy yet.

Recommended minimal version:

- Keep rival pressure derived for one more slice.
- Add visible "rival likely claim" previews to contract and production panels.
- Only persist rival commitments after contract board and production focus are stable.

Deferred:

- Rival inventory, rival warehouses, route ownership, sabotage, auctions, and lasting market share.

Acceptance:

- NPC pressure responds to player production focus and route operations.
- Tests prove deterministic ordering and explanations.
- No save change unless actual commitments are introduced.

## Non-Goals

- No new commodity tier until production focus proves the current 10 goods are fun.
- No fleet/unit ownership yet; route operations remain abstract company operations.
- No full city parcel builder; city development stays role/slot/policy based.
- No combat or map-painting.
- No old-save migration until we decide to support saves across active development branches.

## Verification Plan

For any implementation touching production, logistics, save/load, AI, balance, or Godot runtime:

- `git diff --check`
- `powershell -ExecutionPolicy Bypass -File .\tools\build.ps1`
- `powershell -ExecutionPolicy Bypass -File .\tools\test.ps1`
- `powershell -ExecutionPolicy Bypass -File .\tools\benchmark.ps1`
- `powershell -ExecutionPolicy Bypass -File .\tools\visual-qa.ps1` for Godot UI changes

Because this plan touches economy, logistics, save/load, AI-adjacent pressure, balance, and Godot runtime, implementation should use at least two delegated reviewers before final summary:

- reviewer 1: simulation invariants, determinism, save/hash correctness;
- reviewer 2: Godot integration, tests, benchmark meaning, maintainability.

## Recommended Next Step

Implement Slice 4, Contract Board V0. The scripted season can now win, but the benchmark shows that failures mostly need clearer cargo targets, deadlines, and resource-variety pressure rather than more hidden automation.
