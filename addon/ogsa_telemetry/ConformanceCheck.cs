#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Godot;

namespace Ogsa.Telemetry.Godot;

/// <summary>
/// Cross-engine conformance check: run this scene once per engine plugin.
/// Validates (1) the vendored core decodes the standard repo's golden
/// datagrams to the manifest-declared semantics, and (2) this engine's
/// frame conversion against hand-derived vectors (Godot RH/−Z → OGSA-TS
/// LH/+Z). Prints PASS/FAIL per check and exits non-zero on failure.
///
/// Set CorpusDir to the standard repo's conformance/ folder (absolute
/// path is fine) and run the scene (F6).
/// </summary>
public sealed partial class ConformanceCheck : Node
{
    [Export] public string CorpusDir =
        "../OGSA-Telemetry-Standard/conformance";

    int _pass;
    int _fail;

    void Check(string name, bool ok, string detail = "")
    {
        if (ok) _pass++;
        else _fail++;
        GD.Print($"{(ok ? "PASS" : "FAIL")} {name}{(ok || detail == "" ? "" : $" — {detail}")}");
    }

    public override void _Ready()
    {
        FrameConversionChecks();
        CorpusChecks();
        GD.Print($"conformance: {_pass} passed, {_fail} failed");
        GetTree().Quit(_fail == 0 ? 0 : 1);
    }

    void FrameConversionChecks()
    {
        // position: negate Z
        var p = TelemetryPublisherNode.ToStandard(new Vector3(1f, 2f, 3f));
        Check("frame: position negates Z",
            p.X == 1f && p.Y == 2f && p.Z == -3f);

        // identity → (0,0,0,-1) (≡ identity rotation; q ≡ −q)
        var qi = TelemetryPublisherNode.ToStandard(Quaternion.Identity);
        Check("frame: identity quaternion",
            qi.X == 0 && qi.Y == 0 && qi.Z == 0 && System.Math.Abs(qi.W) == 1f);

        // Godot yaw-left 90°: q=(0,√½,0,√½); standard result must face −X
        var yaw = TelemetryPublisherNode.ToStandard(
            new Quaternion(0f, 0.70710678118f, 0f, 0.70710678118f));
        var fwd = System.Numerics.Vector3.Transform(
            new System.Numerics.Vector3(0, 0, 1), yaw);
        Check("frame: yaw-left faces -X",
            fwd.X < -0.999 && System.Math.Abs(fwd.Y) < 1e-6
                          && System.Math.Abs(fwd.Z) < 1e-6,
            $"got ({fwd.X:F3},{fwd.Y:F3},{fwd.Z:F3})");

        // unit norm preserved
        Check("frame: quaternion norm preserved",
            System.Math.Abs(yaw.Length() - 1.0) < 1e-6);

        // self-inverse roundtrip
        var back = TelemetryPublisherNode.ToGodot(yaw);
        Check("frame: conversion self-inverse",
            System.Math.Abs(back.X - 0f) < 1e-6
            && System.Math.Abs(back.Y - 0.70710678118f) < 1e-6
            && System.Math.Abs(back.Z - 0f) < 1e-6
            && System.Math.Abs(back.W - 0.70710678118f) < 1e-6);
    }

    void CorpusChecks()
    {
        var dir = CorpusDir;
        if (!Directory.Exists(dir))
        {
            GD.Print($"conformance: corpus not found at {Path.GetFullPath(dir)} — set CorpusDir");
            _fail++;
            return;
        }

        var manifest = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(dir, "manifest.json"))).RootElement;
        Check("corpus: manifest schemaVersion 1",
            manifest.GetProperty("schemaVersion").GetInt32() == 1);

        foreach (var v in manifest.GetProperty("vectors").EnumerateArray())
        {
            var id = v.GetProperty("id").GetString()!;
            var file = v.GetProperty("file").GetString()!;
            var hex = string.Concat(
                File.ReadAllLines(Path.Combine(dir, file))
                    .Select(l => l.Trim()));
            var bytes = Convert.FromHexString(hex);

            if (!Ogsa.Telemetry.Envelope.TryUnwrap(
                    bytes, out var type, out var payload))
            {
                Check($"corpus: {id} envelope", false, "TryUnwrap failed");
                continue;
            }
            var root = Capnp.DeserializerState.CreateRoot(
                Ogsa.Telemetry.Envelope.DecodeMessage(payload.Span));
            var ok = type switch
            {
                Ogsa.Telemetry.FrameType.Discovery =>
                    CheckDiscovery(CapnpGen.DiscoveryFrame.READER.create(root)),
                Ogsa.Telemetry.FrameType.Motion =>
                    CheckMotion(CapnpGen.MotionFrame.READER.create(root)),
                Ogsa.Telemetry.FrameType.Event =>
                    CheckEvent(CapnpGen.EventFrame.READER.create(root)),
                _ => false,
            };
            Check($"corpus: {id} decode", ok);
        }
    }

    static bool CheckDiscovery(CapnpGen.DiscoveryFrame.READER d) =>
        d.SchemaVersion == 1
        && d.GameName == "conformance"
        && d.Objects.Count == 1;

    static bool CheckMotion(CapnpGen.MotionFrame.READER m) =>
        m.Timestamp > 0 && m.Objects.Count is 1 or 2
        && m.Objects[0].Name != null;

    static bool CheckEvent(CapnpGen.EventFrame.READER e) =>
        e.Events.Count == 1 && e.Events[0].Name == "body.impact"
        && System.Math.Abs(e.Events[0].Intensity - 0.75f) < 1e-6;
}
