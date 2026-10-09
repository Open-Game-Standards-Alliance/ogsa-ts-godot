# sync-addon.ps1 — mirror this addon into a Godot project's addons/ folder.
#
# Godot test projects consume a *copy* of addons/ogsa_telemetry, which
# silently drifts behind this repo. Run this after every addon change:
#
#   ./sync-addon.ps1 -Project "C:\path\to\MyGodotProject"
#
# -Project accepts the project directory or its project.godot.
# Mirrors addon/ogsa_telemetry exactly (stale files in the copy are
# deleted); .godot/ caches are left alone — Godot reimports on launch.

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Project
)

$ErrorActionPreference = 'Stop'

$repoDir = $PSScriptRoot
$src = Join-Path $repoDir 'addon\ogsa_telemetry'
if (-not (Test-Path (Join-Path $src 'plugin.cfg'))) {
    throw "not run from the repo root: $src missing"
}

if ($Project.EndsWith('project.godot')) { $Project = Split-Path $Project -Parent }
$Project = (Resolve-Path $Project).Path
if (-not (Test-Path (Join-Path $Project 'project.godot'))) {
    throw "no project.godot found in $Project"
}

$dst = Join-Path $Project 'addons\ogsa_telemetry'
# /XF *.uid: Godot generates .uid sidecars in the project copy (like Unity's
# .meta files) — preserve them so references stay stable and no reimport churn
robocopy $src $dst /MIR /NJH /NDL /NP /XF *.uid
if ($LASTEXITCODE -ge 8) { throw "robocopy failed ($LASTEXITCODE)" }
Write-Host "synced -> $dst"
Write-Host "note: .godot/ caches may reimport on next editor launch (normal)."
exit 0
