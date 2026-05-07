# 2026-05-07 Next Feature Research Plan

## Summary

Completed a focused research and planning pass for the next gameplay layer after the integrated testing-ready branch and overlay clarity pass. The recommendation is to implement a company-operations loop centered on Production Focus + Flow Throughput, starting with route-operation throughput metrics before adding persisted production focus state.

## Changed Systems

- `docs/agent-plans/next-feature-company-operations-plan.md`: new research-backed implementation plan covering current gaps, external references, recommended slices, non-goals, verification, and review expectations.
- `PROJECT_MEMORY.md`: records the recommended next feature direction, changed planning artifact, residual production/save risk, and updated next step.

## Tests

- No new verification run was needed for this planning-only change.
- The immediately preceding overlay clarity pass on 2026-05-07 already passed `tools/build.ps1`, `tools/test.ps1`, and `tools/visual-qa.ps1`.

## Review Notes

- The plan keeps the simulation core Godot-free and explicitly defers fleets, combat, commodity expansion, full rival inventory, and parcel city building.
- The main implementation risk is save version churn: persisted production focus probably needs save version 5 and an ADR.
- The first implementation slice intentionally avoids save changes by adding throughput telemetry and benchmark metrics first.

## Risks

- Production focus can make the economy feel more intentional, but it can also expose balance weakness in the current 10-good/6-recipe content set.
- `PrototypeSession` is already large, so implementation should avoid burying more orchestration there without considering a Godot-free service extraction.
- Benchmark scenario win rate should be reported separately for naive auto-play and scripted production/route play so balance signals remain honest.

## Next Step

Implement route-operation flow throughput metrics before adding production focus save state.
