# Stage 5: NPC Company AI

## Status

Implemented first deterministic NPC pressure slice on `codex-stage5-deterministic-npc-pressure`.

## Context Read

- Start from `agent/mvp-roadmap-execution`.
- NPC behavior must remain deterministic and explainable.
- Coordinate with production chain and route operation surfaces.
- `PROJECT_MEMORY.md` confirms the current AI surface is still a small deterministic utility scorer surfaced as `PrototypeSnapshot.AiChoice` and an "AI" ledger entry.
- ADR-0001 keeps AI in plain .NET core/bridge code, not Godot. ADR-0002 requires deterministic save/hash behavior and has a separate AI RNG stream if randomness is ever introduced. ADR-0004 favors discrete tick explanations. ADR-0006 means warehouse and route policy state must influence any AI opportunity model through deterministic inputs.

## Findings

- Current implementation is intentionally minimal: `AI.Company.CompanyUtilityAi` scores `Opportunity` records by revenue, capital cost, transport risk, volatility, and strategic bonus; `PrototypeSession.RunAi` picks the best opportunity and applies a small negative cash pressure ledger entry.
- This is a good seed, but it is not yet a visible NPC company model. It has no company identity, no action vocabulary, no reason breakdown, and no stable UI-facing action summary beyond the winning opportunity id.
- The correct Stage 5 shape should be a deterministic pressure system: NPC companies do not need to own full assets in the first MVP slice, but their moves should make the player feel contested on routes, resources, and production chains.
- Stage 5 should wait for Stage 3/4 to provide stable opportunity ids and expected-value fields before replacing `BuildOpportunities`; otherwise the AI would hard-code assumptions that the next agents are actively changing.

## Deterministic NPC Company Plan

Model NPCs as named company profiles evaluating the same visible opportunity surface the player sees. Each company produces one `NpcActionSummary` per tick or "none" if every score is non-positive.

Recommended core records for the later implementation:

- `NpcCompanyProfile`: `companyId`, display name, strategy tag, capital posture, preferred resources, preferred route modes, risk tolerance, and deterministic sort order.
- `NpcActionCandidate`: stable `candidateId`, action kind, route id, source city id, target city id, resource id, expected gross, expected cost, expected net, capacity used, unmet demand served, policy pressure, and explanation inputs.
- `NpcActionScore`: candidate id, company id, total score, ranked components, and deterministic tie-break key.
- `NpcActionSummary`: tick, company id, action kind, target ids, resource id, score, pressure amount, one-line explanation, and top scoring factors.

Keep the first implementation derived from current simulation state only. Do not persist NPC company state in save v2 unless a later design needs lasting commitments, budgets, claims, or cooldowns. If NPC state becomes persistent, add a save-format ADR and migration plan before implementation.

## Action Vocabulary

Use a small vocabulary that maps cleanly to player concepts and ledger rows:

- `ContestRoute`: NPC pressures a route-operation candidate that the player could also use.
- `BidForCargo`: NPC raises pressure on a resource shipment or contract serving high unmet demand.
- `BackProductionChain`: NPC pressures a Stage 3 production-chain opportunity, such as inputs flowing into an output with strong margins.
- `SecureSupply`: NPC targets a city/resource shortage where warehouse policy, reorder point, or safety stock indicates urgency.
- `HoldPosition`: no positive action; summary explains that margins, capacity, or demand were insufficient.

Avoid actions that imply asset ownership, sabotage, warfare, map painting, or hidden random events. The MVP thesis is pressure through flows.

## Scoring Inputs

Stage 5 should score only inputs already exposed by deterministic systems:

- Revenue and margin: expected gross, transport cost, production input cost, expected net.
- Demand pressure: scarcity, unmet demand served, desired stock gap, consumption per tick.
- Warehouse policy: safety stock, reorder point, policy mode, shipment priority, source exportable units.
- Route policy and operations: allowed or blocked cargo, priority resource, capacity used, route mode, lead/cost proxy, recurring order value.
- Production chains: recipe id, input availability, output value, city role/specialization modifiers once Stage 2/3 finalize them.
- Risk and volatility: route mode risk, resource volatility, perishability if content exposes it.
- Strategy fit: company profile preference for resource tags, route modes, staples/luxuries, or shortage relief.

Scoring should use integer points or decimals rounded once at the boundary. Do not use current culture formatting, wall-clock time, unordered dictionary iteration, or unseeded randomness. Sort candidates by score descending, then `companyId`, action kind, `candidateId`, route id, city id, and resource id using ordinal comparison.

## UI And Ledger Surfacing

The Godot layer should receive ready-to-render summaries from the bridge; it should not recompute AI reasons.

- Sidebar metric: replace the opaque "AI Move" id with company name, action kind, target resource, and score.
- Event ledger: emit rows like `T12 AI: North Sea Co. contests route_003 grain; shortage +18, net +11.40, policy priority +6`.
- Inspector: route/city panels should show the latest related NPC action if the target route, city, or resource matches.
- Map pressure: later UI can tint the route/city or show a small pressure mark, but this is presentation only and must not enter save state.
- Test bench: include the top action id and explanation fingerprint so visual smoke can prove the summary is present without parsing full prose.

## Tests To Add With Implementation

- `CompanyUtilityAi` or successor scorer chooses the highest score and returns a stable reason breakdown.
- Equal-score candidates break ties by ordinal ids, not input order.
- Repeated sessions with the same seed produce the same NPC summaries, ledger rows, cash pressure, and save hash.
- Warehouse policy changes can alter AI scoring only through exposed policy/scarcity/exportability inputs.
- Route policy blocking removes or lowers matching `ContestRoute`/`BidForCargo` candidates deterministically.
- Stage 3 production-chain opportunities rank deterministically and include input/output reason factors.
- No positive candidates yields `HoldPosition`, zero pressure, and no misleading cash penalty.
- Save/load/hash test remains stable for non-persistent AI summaries; if persistent NPC state is introduced, add save validation tests first.

## Safe First Slice

After Stage 3/4 land their candidate surfaces, implement the smallest playable slice:

1. Add pure `AI.Company` records for candidate, score breakdown, and action summary.
2. Adapt the existing bridge `BuildOpportunities` path to consume Stage 4 route-operation candidates and Stage 3 production-chain candidates instead of raw warehouse-stock guesses.
3. Keep one deterministic NPC company profile and one action per tick.
4. Surface the action summary in `PrototypeSnapshot`, the sidebar metric, the event ledger, and route/city inspectors.
5. Keep save format unchanged by deriving summaries each tick from deterministic state and recording only existing ledger/company cash effects.
6. Add focused console tests first, then Godot interaction/visual smoke only after UI text is wired.

## Dependencies And Blockers

- Stage 3 should expose stable production-chain opportunity ids, expected net/margin, involved city/resource/recipe ids, and reason factors.
- Stage 4 should expose stable route-operation candidate ids, route policy effects, capacity used, expected profit, unmet demand served, and blocked/priority cargo effects.
- Stage 2 city specialization can be a scoring bonus later, but Stage 5 should not block on it for the first slice.
- Stage 6 goal loop may read NPC pressure as score context, but Stage 5 should not create scenario objectives.

## Non-Goals For MVP Slice

- No hidden NPC inventory, asset ownership, loans, diplomacy, sabotage, or war-game behavior.
- No new save version unless NPCs gain persistent state.
- No Godot dependency in `AI.Company`.
- No broader economic rebalancing while adding the first NPC pressure slice.

## Implemented Slice

- Added pure `NpcPressureCandidate`, `NpcPressureScore`, and `DeterministicNpcPressureAi` records/scorer in `AI.Company`.
- Replaced the bridge's old raw warehouse-stock AI move path with a derived NPC pressure surface based on Stage 4 `RouteOperationCandidates` and Stage 3 `ProductionChainOpportunities`.
- Added `PrototypeNpcPressureView` and `PrototypeSnapshot.NpcPressures`; pressure is sorted deterministically by pressure, shipment priority, expected value, company, intent, and stable id.
- Non-dispatchable route operations now produce zero scored pressure instead of cash pressure; production pressure keeps the source city as `CityId` and exposes destination context separately.
- Kept save format unchanged. NPC pressure has no persisted company budget, claims, route ownership, cooldowns, inventory, or hidden state.
- Godot now shows NPC Pressure in the Company Ledger metric, a sidebar section, the system probe, and selected city/route inspectors.
- Interaction smoke and visual QA assert the NPC Pressure surface renders a concrete `North Sea Company` pressure line.

## Verification

- `powershell -ExecutionPolicy Bypass -File .\tools\build.ps1`: passed with 0 warnings and 0 errors.
- `powershell -ExecutionPolicy Bypass -File .\tools\test.ps1`: passed with 47/47 console tests, `INTERACTION_SMOKE PASS`, and `VISUAL_SMOKE PASS`; latest visual smoke frame `artifacts/godot-smoke/visual-smoke-20260430-19532700000002.png`.
- `powershell -ExecutionPolicy Bypass -File .\tools\benchmark.ps1`: passed with 25/25 playable seeds, average unmet demand ratio 0.7115, median time to profit 1.0, and bankruptcy frequency 0/25.
- `powershell -ExecutionPolicy Bypass -File .\tools\visual-qa.ps1`: passed with 18 captures in `artifacts/godot-visual-qa/visual-qa-20260430-195421`.

## Review

- Delegated simulation review found one P1: blocked route operations could still produce positive NPC cash pressure. Fixed by making non-contestable pressure candidates score zero and adding a blocked-route pressure test.
- Delegated simulation review found one P2: production pressure used the demand destination as its city context. Fixed by keeping `CityId` anchored to the production source and adding target city context.
- Delegated integration review returned GO with P2s for broad UI assertions and missing direct scorer fixtures. Fixed by naming/scanning the NPC pressure log, adding a dedicated visual QA capture, and adding a direct tie/blocked scorer test.

## Handoff Notes

- The scorer currently ranks source-production pressure very strongly across the benchmark corpus. This is acceptable for the first derived-pressure slice, but the next balance pass should add company strategy profiles before pressure becomes a persistent NPC commitment.
- Full NPC ownership, budgets, cooldowns, claims, diplomacy, or long-lived actions remain out of scope until a save-format ADR is added.
