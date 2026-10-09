# ogsa-ts-godot working conventions

- Core is vendored under `addon/ogsa_telemetry/Core/` from
  `../ogsa-ts-dotnet` (single source of truth). Never hand-edit vendored
  files; refresh with `./sync-core.sh`.
- The core must stay C# 9 / netstandard2.1-clean; the dotnet repo's
  multi-target build + tests are the gate.
- The Godot wrapper is the only engine-facing surface: exported config,
  Godot-typed overloads, RigidBody3D auto-bind, _Process tick with
  Time.GetTicksMsec()/1000.0.
- The frame conversion (RH/−Z → LH/+Z: negate Z; quaternion
  (x,y,−z,−w)) lives in TelemetryPublisherNode and must stay covered by
  ConformanceCheck's hand-derived vectors — run it after every core sync.
- Godot test projects consume a *copy* under <project>/addons/ogsa_telemetry —
  after changing the addon run ./sync-addon.ps1 -Project <path> so the copy
  never drifts (preserves Godot-generated .uid sidecars).
- Product docs describe current state only; history lives in the
  Open-Game-Standards-Alliance knowledge base (knowledge/README.md bridge).
