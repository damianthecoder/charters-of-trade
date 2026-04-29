# ADR-0002: Determinism And Save Format

## Status

Accepted

## Decision

Persist `saveVersion`, `contentHash`, `worldGenVersion`, `worldSeed`, separate RNG stream states, and a delta of mutable game state.

## Context

The game needs reproducible seeds, replayable tests, stable benchmarks, and future mod compatibility.

## Consequences

- Save files can detect incompatible content or generator versions.
- Tests can verify `save-load-save` by comparing stable hashes.
- RNG streams remain separated for world generation, events, and AI.

