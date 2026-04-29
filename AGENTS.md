# Charters of Trade Agent Instructions

Before changing code, read `PROJECT_MEMORY.md` and the relevant ADRs in `docs/adr`.

## Permanent Rules

- Keep the simulation core free of Godot dependencies.
- Update `PROJECT_MEMORY.md` whenever world generation, economy, logistics, save/load, AI, balance, CI, or benchmarks change.
- Add an ADR for major architecture, data, determinism, save format, or tooling decisions.
- Add a research note in `docs/research` when an implementation problem blocks work for more than 15 minutes.
- Prefer official documentation first, then GitHub issues, engine/tool forums, Reddit, technical blogs, and GDC materials.
- For larger changes, automatically delegate separate review agents before the final session summary. A review agent does not implement changes; it checks the diff, tests, risks, bugs, regressions, architecture violations, and missing coverage. Any change touching world generation, economy, logistics, save/load, AI, balance, CI, or Godot runtime requires at least one review agent. Cross-cutting changes touching 3+ subsystems require two reviewers: one focused on simulation bugs/invariants and one focused on integration, tests, and maintainability.
- Do not expand the MVP scope without recording the decision.
- Use `tools/build.ps1`, `tools/test.ps1`, and `tools/benchmark.ps1` for local verification; these scripts keep .NET/NuGet caches inside the workspace and disable restore graph parallelism.
- Use `tools/godot.ps1` for Godot .NET CLI calls. It points at the installed Godot 4.6.1 .NET console build on the OneDrive Desktop.

## Memory Keeper Ritual

- Treat the "always-on agent" as a recurring project ritual, not a background daemon. For small changes, the main agent acts as memory keeper. For larger or cross-cutting work, delegate a separate memory keeper agent whose only job is to track decisions, changed systems, tests, review findings, risks, and the next step.
- The memory keeper must not implement gameplay or refactor code. It reads the working context, observes the intended changes, and prepares memory/checkpoint updates.
- Before the final session summary, update `PROJECT_MEMORY.md` and add or update a checkpoint in `docs/checkpoints` whenever the session changed any key system, test result, benchmark, tooling rule, review finding, or project risk.
- Checkpoints should record: summary, changed systems, tests run with concrete results, review notes and whether each was fixed/deferred, risks, and exactly one next step.
- If context compaction happens, the next session must be able to resume from `PROJECT_MEMORY.md`, `AGENTS.md`, and the latest checkpoint without relying on chat history.

## Project Thesis

Charters of Trade is a graph-first mercantile strategy game about building company value through control of flows. It is not a war game, a map-painting grand strategy game, or a full parcel city builder.
