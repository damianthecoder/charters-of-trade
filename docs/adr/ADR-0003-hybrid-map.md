# ADR-0003: Hybrid Raster And Node Graph Map

## Status

Accepted

## Decision

Represent terrain, biomes, and movement cost as raster/tile data, but run economy and logistics on a node graph.

## Context

The game needs to feel like a world without making every tile part of the economy tick.

## Consequences

- World generation can use terrain fields to place settlements and route candidates.
- Economy ticks remain bounded by cities, warehouses, markets, recipes, and route edges.
- Visual map streaming can evolve separately from simulation scale.

