#!/usr/bin/env bash
# sync-core.sh — vendor the engine-agnostic core from ../ogsa-ts-dotnet.
# Run from the repo root after pulling upstream changes: ./sync-core.sh
# Godot needs no DLL vendoring: the game csproj pulls Capnp.Net.Runtime
# from NuGet (see README install steps).
set -euo pipefail
SRC="../ogsa-ts-dotnet/src/Ogsa.Telemetry"
DST="addon/ogsa_telemetry/Core"

changed=0
for f in "$SRC"/*.cs; do
  if ! cmp -s "$f" "$DST/$(basename "$f")"; then
    cp "$f" "$DST/"
    echo "updated: $DST/$(basename "$f")"
    changed=1
  fi
done
for f in "$SRC"/Schema/*.cs; do
  if ! cmp -s "$f" "$DST/Schema/$(basename "$f")"; then
    cp "$f" "$DST/Schema/"
    echo "updated: $DST/Schema/$(basename "$f")"
    changed=1
  fi
done
# guard against Unity-style meta leftovers (Godot uses none)
find "$DST" -name "*.meta" -delete
[ "$changed" -eq 0 ] && echo "core up to date"
