# Agent Plan: Visual UX Map Modes

## Mission

Improve the Godot presentation layer so the P0 prototype reads as a mercantile flow map: clearer Ledger Cartography styling, map modes, route/city inspector improvements, warning symbols, and route contract controls wired to the bridge surface.

Work on branch:

```powershell
git checkout main
git pull
git checkout -b agent/visual-ux-map-modes
```

## Ownership

Primary files:

- `src/ChartersOfTrade.Godot/Scripts/BootstrapPanel.cs`
- `src/ChartersOfTrade.Godot/scenes/Main.tscn`
- Godot-only visual assets/styles if added
- relevant visual docs/checkpoints

Avoid editing:

- `src/GodotBridge/PrototypeSession.cs` except for compile-time adaptation to the systems agent's already-merged public surface.
- Core simulation projects.
- Tests for simulation behavior unless coordinating after the systems branch lands.

You are not alone in the codebase. Another agent may be changing route contract behavior in `GodotBridge`. Do not revert their work. Build against the shared bridge contract and keep UI state in the Godot presentation layer.

## Required Context

Before editing, read:

- `PROJECT_MEMORY.md`
- `AGENTS.md`
- `docs/adr/ADR-0001-godot-dotnet-core.md`
- `docs/adr/ADR-0003-hybrid-map.md`
- `docs/research/2026-04-29-visual-layer.md`
- `docs/checkpoints/2026-04-29-visual-flow-map-slice.md`

## Shared Bridge Contract

Consume this surface when available:

```csharp
PrototypeSnapshot.AvailableContracts
PrototypeSnapshot.SelectedContractId
PrototypeSession.SelectRouteContract(string contractId)
```

Expected contract fields:

```csharp
Id
RouteId
FromNode
ToNode
ResourceId
ExpectedRevenue
TransportCost
ExpectedNet
CapacityPerDay
```

If the systems branch is not merged yet, build UI in a way that degrades gracefully: show a disabled route contract control or a placeholder message, but do not invent separate state that conflicts with the bridge contract.

## Visual Direction

Use the Ledger Cartography direction:

- Territory is quiet.
- Routes, markets, margins, capacity, and supply pressure are visually primary.
- Avoid map-painting signals and warlike flags.
- Use merchant-ledger, portolan-map, ink, stamp, brass, and paper references.
- Keep information readable before decorative detail.

Do not let the UI become a one-color brown/sepia interface. Use restrained accents: patina green, ink blue, muted red seal, ochre, brass, and paper tones.

## Tasks

1. Improve the map and sidebar visual hierarchy.
2. Add or refine city type stamps/icons:
   - charter town
   - port
   - market town
3. Add map modes:
   - `Routes`: route mode/capacity emphasis
   - `Profit`: route color/labels emphasize latest net cash
   - `Demand`: city supply pressure and unmet demand emphasis
4. Preserve hover and selected states across map modes and ticks.
5. Add a route contract UI area:
   - selected route contract summary
   - available contracts for selected route/city context
   - action button that calls `PrototypeSession.SelectRouteContract`
   - disabled/placeholder state if contract data is absent
6. Improve inspector structure:
   - city: stock, warehouse, supply pressure, connected routes, recent effects
   - route: endpoints, mode, capacity, cost, latest cashflow, contract option
7. Add warning symbols for:
   - city unmet demand
   - route losing money
   - route capacity/pressure if data is available
8. Keep all selection, hover, mode, and visual animation state in Godot UI.
9. Update visual docs/checkpoints if the visual language changes materially.

## Ready Criteria

This agent is done when:

- The Godot prototype has clearly improved visual hierarchy.
- Map modes work and answer distinct player questions.
- Route and city inspectors remain usable after `Advance Tick` and `Run 5`.
- Route contract controls are visible and either wired to the bridge method or gracefully disabled until the systems branch lands.
- No Godot concepts leak into core or `GodotBridge` models beyond Godot-free view data.
- `powershell -ExecutionPolicy Bypass -File .\tools\build.ps1` passes.
- `powershell -ExecutionPolicy Bypass -File .\tools\test.ps1` passes, including Godot smoke.

## Manual QA

Run Godot and verify:

- scene starts
- map is nonblank
- city hover works
- route hover works
- click city updates inspector
- click route updates inspector
- map mode switch preserves selection
- `Advance Tick` preserves valid selection
- `Run 5` preserves valid selection and does not break inspector
- route contract control is visible and behaves as expected for available/disabled states

## Commit Plan

Prefer small commits:

```text
Improve flow map visual language
Add map modes for routes profit and demand
Add route contract controls to inspector
Polish warnings and ledger cartography styling
Record visual UX checkpoint
```

Push the branch:

```powershell
git push -u origin agent/visual-ux-map-modes
```

## Review Requirements

This touches Godot runtime, so request at least one review focused on:

- Godot scene startup
- UI state consistency
- no core dependency leakage
- performance risks from per-frame redraw
- missing interaction smoke coverage

If this branch also adapts bridge behavior after the systems branch lands, request a second reviewer for integration and tests.

## Handoff Notes

Merge after the route contract system branch if possible. If this branch lands first, keep contract controls disabled or placeholder-only, then follow up with a small integration commit once `AvailableContracts`, `SelectedContractId`, and `SelectRouteContract` exist.
