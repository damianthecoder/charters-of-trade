# ADR-0004: Discrete Economy Tick

## Status

Accepted

## Decision

Use a daily or weekly deterministic simulation tick while allowing the presentation layer to animate continuously.

## Context

The design needs readable economic causality, replayable tests, and useful balancing tools.

## Consequences

- UI can explain why money, stock, or prosperity changed per tick.
- Benchmarks and soak tests can run without rendering.
- Animation and simulation remain decoupled.

