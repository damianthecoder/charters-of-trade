# Stage 2: City Specialization

## Status

Implemented bounded bridge slice.

## Current Notes

- Start from `agent/mvp-roadmap-execution`.
- Keep city specialization deterministic and Godot-free.
- Coordinate before editing shared bridge/UI files.

## Findings

- `PrototypeSession` already builds deterministic runtime cities from sorted `WorldNode` ids.
- Existing runtime city districts are not exposed in `PrototypeCityView`.
- Safe slice: expose city districts and a deterministic `PrototypeCitySpecialization` derived from `WorldNode.Kind` plus node resources. No Godot UI, save format, worldgen, or economy changes planned.

## Implemented Slice

- Added bridge-only `PrototypeCitySpecialization` with role id, label, anchor resources, output resources, and rationale.
- Exposed existing city districts and specialization through `PrototypeCityView`.
- Derived roles deterministically from `WorldNode.Kind` and sorted node resources.
- Added focused test coverage for same-seed determinism, district exposure, anchor-resource validity, and tick stability.
- Fixed delegated-review P3 feedback by exposing district and specialization resource lists as read-only snapshot copies.

## Verification

- `powershell -ExecutionPolicy Bypass -File .\tools\build.ps1`: passed with 0 warnings and 0 errors.
- `powershell -ExecutionPolicy Bypass -File .\tools\test.ps1`: passed with 37/37 console tests, `INTERACTION_SMOKE PASS`, and `VISUAL_SMOKE PASS`; latest visual frame `artifacts/godot-smoke/visual-smoke-20260430-15154800000002.png`.
- `powershell -ExecutionPolicy Bypass -File .\tools\benchmark.ps1`: passed with 25/25 playable seeds, average unmet demand ratio 0.7115, median time to profit 1.0, and bankruptcy frequency 0/25.

## Review

- Delegated Stage 2 review found no blockers.
- Review confirmed Godot-free boundary, deterministic role derivation, no save-format/ADR requirement, and adequate bounded tests.
- Review note for Stage 3: empty `OutputResources` on `charter_hub` or `market_exchange` means no role bonus/default availability, not "this city can produce nothing."

## Handoff Notes

- No ADR added: this is descriptive bridge metadata, not a major architecture/save/determinism decision.
- No Godot UI touched.
- Stage 3 can consume `PrototypeCitySpecialization.OutputResources` for visible production-chain opportunities if coordination accepts that direction.
