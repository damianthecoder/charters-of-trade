# 2026-04-29 Memory Keeper Ritual Checkpoint

## Summary

Added the always-on documentation approach as a recurring project ritual rather than a background daemon. The goal is to preserve context through compaction by keeping repo files as the source of truth.

## Changed Systems

- Process: `AGENTS.md` now defines the memory keeper ritual.
- Project memory: `PROJECT_MEMORY.md` records the decision that memory keeping is a recurring agent role, not a daemon.

## Tests Run

- Not run; documentation/process-only change.

## Review Notes

- Not required; this is not a code or architecture change.

## Risks

- The ritual depends on agents consistently updating memory before final summaries.
- If a future session skips `PROJECT_MEMORY.md`, `AGENTS.md`, or the latest checkpoint, compaction resilience degrades.

## Next Step

Use the memory keeper ritual during the next larger implementation pass: route/market selection, route contract choice, and cash-flow explanation panel.
