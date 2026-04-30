# Stage 3: Production Chain Gameplay

## Status

Design ready; waiting on Stage 2 city specialization surfaces before gameplay code edits.

## Current Notes

- Start from `agent/mvp-roadmap-execution`.
- Focus on player-readable production chain opportunities.
- Coordinate dependencies on city roles and route operations.
- This pass intentionally changes only this communication file. Gameplay implementation should wait until Stage 2 settles shared city/bridge surfaces.

## Findings

### Existing surfaces to build on

- P0 content already defines resources and recipes in `content/resources.p0.json` and `content/recipes.p0.json`, loaded through `GameContentLoader` with deterministic content hashes.
- `RecipeDef` already carries `Id`, `BuildingType`, `Inputs`, `Outputs`, `Workforce`, `BaseDays`, and `RequiresTech`.
- `PrototypeSession` already runs deterministic production each tick, but the player only sees a generic ledger entry like "recipes produced".
- `PrototypeSnapshot` already exposes resources, routes, prices, cities, market signals, route contracts, selected route contract, and route policies. A production-chain view can follow this snapshot pattern without Godot dependencies.
- Current production consumes company warehouse inputs and moves outputs to the local market for a small cash gain. It does not yet explain missing inputs, city fit, recipe margin, workforce fit, or where output demand exists.

### Concrete production-chain gameplay design

The MVP production layer should be an opportunity reader before it becomes a new command system. Each tick, the player should be able to answer:

- What can this city make?
- What inputs are missing or protected by warehouse policy?
- What output does it create, who wants it, and why is it profitable or not?
- Which route or city action would unblock the chain?

Add a deterministic list of `PrototypeProductionChainOpportunityView` entries to the bridge snapshot after Stage 2 lands. The view should be read-only in the first slice and derived from existing state:

- `Id`: stable key, e.g. `{cityId}:{recipeId}`.
- `CityId`, `CityName`, `RecipeId`, `BuildingType`.
- `Inputs`: resource lines with required amount, warehouse amount, market amount, protected warehouse reserve, available amount, missing amount, and local unit price.
- `Outputs`: resource lines with produced amount, local unit price, local scarcity, best known destination city id, best known destination price, and destination shipment priority.
- `MaxRunsFromWarehouse`: integer count from available unprotected warehouse inputs. For zero-input source recipes, cap to `1` for display and label as source production.
- `InputCost`, `OutputValue`, `ExpectedMargin`: decimal values rounded with invariant deterministic money rules. Input cost should use local replacement price, not historical purchase price.
- `BottleneckResourceId`: first missing or protected input after deterministic ordering.
- `Score`: order key that combines output scarcity, expected margin, input completeness, and route/export fit.
- `Reason`: concise player-facing explanation such as `ready: bread margin +8.40`, `missing grain 2`, `wood protected by safety stock`, or `output surplus; route export needed`.

Ordering should be deterministic:

1. Ready chains before blocked chains.
2. Higher destination shipment priority.
3. Higher expected margin.
4. Fewer missing input units.
5. `CityId`, then `RecipeId` using ordinal string order.

The first implemented slice should not add save state. Selecting or pinning a production focus would become gameplay state and should wait for a save/hash decision, probably a small ADR or ADR-0006 follow-up if it persists across saves.

### Data and API needs

- Add `PrototypeSnapshot.ProductionChainOpportunities` as a read-only list.
- Add `PrototypeProductionChainOpportunityView` and a compact `PrototypeProductionResourceLineView` in `GodotBridge`, matching the existing route contract and market signal view style.
- Keep the calculator Godot-free. For the first slice it can live inside `PrototypeSession` beside `BuildAvailableContracts`; if Stage 5 needs the same scoring for NPC AI, move the pure scoring function into `Economy.Core`.
- Use existing `ResourceDef`, `RecipeDef`, `MarketNeed`, `MarketPrice`, `PrototypeMarketSignal`, `TradeRoute`, and `PrototypeRoutePolicyView` data. No new content format is needed for the first slice.
- Need Stage 2 to expose city role/capability data before final gating. Minimum useful contract: for each city, a stable role id/name plus allowed or favored recipe ids/building types. If Stage 2 does not supply role gating in time, Stage 3 should mark every recipe as `available by prototype default` and keep role bonuses out of the first patch.
- Need Stage 4 to expose recurring route operation shape before production opportunities claim import/export availability. For the first slice, route fit should be explanatory only: "best destination" and "candidate route" from current routes, route policies, capacity, and shipment priority.

### Tests to add with implementation

- `prototype production chain opportunities are deterministic`: create two sessions with the same seed and compare a culture-invariant opportunity fingerprint across city, recipe, score, margin, bottleneck, and order.
- `prototype production chain opportunities explain inputs and outputs`: assert at least one P0 recipe exposes required inputs, output value, margin, and a non-empty reason.
- `prototype production chain opportunities order ready chains first`: use seed/state setup where one recipe is runnable and another is missing inputs; assert ready appears first before margin tie-breakers.
- `prototype production chain opportunities respect warehouse reserve`: set a high warehouse policy on an input resource and assert the opportunity reports protected stock/missing input instead of claiming it is fully runnable.
- `prototype production chain opportunities expose destination demand`: assert manufactured outputs can point to a destination city or explain local surplus when no route-policy-compatible destination exists.
- If Stage 2 adds role gating before this implementation, add `city specialization gates production chain opportunities deterministically`.
- If Stage 4 lands recurring operations first, add a test proving blocked route resources remove import/export route suggestions from the opportunity explanation.

### UI surface expectations

- Add a `Production Chains` section to the existing sidebar, near `Market Pressure` and `Warehouse Policy`.
- When no city is selected, show the top global opportunities. When a city is selected, filter to that city and show all available chains sorted by the deterministic order.
- Each row should show recipe/building, input readiness, output resource, expected margin, and one bottleneck/action phrase. Example: `Bake bread | grain 0/2 | +8.40 | import grain`.
- The selected city inspector should include the top chain and bottleneck. This should not create a new map mode in the first slice.
- A later UI pass can add a production map mode coloring cities by best chain margin or bottleneck, but that is outside the 1-day slice unless Stage 2/4 finish early.
- Do not expose invisible debug math in long prose. Keep the UI scannable: resource ids, counts, margin, and short reasons.

### Dependencies and coordination

- Stage 2 dependency: city roles/specializations determine whether `RecipeDef.BuildingType` is allowed, favored, blocked, or merely displayed as future potential. Stage 3 should not edit shared city view records until Stage 2 publishes its city API.
- Stage 4 dependency: route operations determine whether chain opportunities can reserve recurring imports/exports. Stage 3 should only suggest route candidates until Stage 4 stabilizes operation commands and save implications.
- Stage 5 dependency: NPC AI can reuse the opportunity scoring once deterministic tests exist. Avoid UI-only scoring that the AI cannot inspect later.
- Stage 6 dependency: goal-loop objectives may use production-chain readiness, margin, or served output demand. Keep opportunity ids stable enough for objective summaries.
- Save/load: read-only opportunity views require no save change. Any production focus, pinned chain, build order, or recurring production order must be treated as gameplay state and reviewed for save/hash impact.

### 1-day implementation slice after Stage 2

1. Add read-only bridge records and `PrototypeSnapshot.ProductionChainOpportunities`.
2. Implement deterministic opportunity calculation from current cities, recipes, prices, market signals, warehouse reserves, route policies, and Stage 2 city role data if present.
3. Add the deterministic ordering and explanation tests above, focusing on bridge-level behavior first.
4. Add a compact Godot sidebar section listing top production chains and selected-city chains.
5. Run `tools/build.ps1`, `tools/test.ps1`, `tools/benchmark.ps1`, and `tools/visual-qa.ps1` if the Godot UI changes.

Review need for that slice: at least one delegated reviewer because it will touch economy-facing bridge behavior and Godot UI; two reviewers if it also consumes Stage 2 city-role data and Stage 4 route-operation APIs in the same patch.
