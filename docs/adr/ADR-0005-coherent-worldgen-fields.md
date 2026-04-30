# ADR-0005: Coherent Terrain Fields For Visual Verification

## Status

Accepted

## Decision

Generate P0 terrain from deterministic coherent value-noise fields plus a border-water landmass falloff, rather than independent per-tile random samples.

## Context

The prototype map is a test instrument for economy and logistics. The previous terrain was deterministic, but it looked like noisy blocks and made visual verification of coastlines, route topology, settlement placement, and city pressure hard to read.

## Consequences

- World generation remains Godot-free and deterministic.
- The world hash still includes the terrain raster, so generator changes remain visible to save/test tooling.
- `WorldGenVersion` advances to `0.2.0` because the default generated worlds change.
- Future map art can improve rendering without changing the economy graph contract.
