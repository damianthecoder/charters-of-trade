# ADR-0001: Godot .NET With Pure Simulation Core

## Status

Accepted

## Decision

Use Godot 4.x .NET for the presentation layer and keep the simulation in plain .NET projects with no Godot dependencies.

## Context

The project prioritizes deterministic simulation, moddable data, PC-first distribution, and a manageable indie scope. Godot .NET fits the licensing and modding preferences, while a pure .NET core keeps testing and benchmarking fast.

## Consequences

- The core can run under `dotnet test`, custom runners, and future headless tools without opening Godot.
- Godot handles camera, input, UI, rendering, audio, asset loading, and scene composition.
- Web export is out of scope because Godot C# does not support web export in stable documentation.

