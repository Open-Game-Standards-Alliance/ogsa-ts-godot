#nullable enable
using Godot;

namespace Ogsa.Telemetry.Godot.Demo;

/// <summary>Demo vehicle: moves in a circle feeding rpm/speed/headlights
/// channels. Pair with ogsa-ts-dotnet's TelemetryReceive.</summary>
public sealed partial class DemoVehicle : Node3D
{
    TelemetryPublisherNode? _telemetry;
    Ogsa.Telemetry.ChannelHandle _rpm;
    Ogsa.Telemetry.ChannelHandle _speed;
    Ogsa.Telemetry.ChannelHandle _headlights;

    double _t;
    const float Radius = 10f;
    const float AngularSpeed = 0.8f; // rad/s
    const float MaxRpm = 6500f;

    public override void _Ready()
    {
        _telemetry = GetNode<TelemetryPublisherNode>("%TelemetryPublisher");
        _rpm = _telemetry.DeclareChannel(
            "engine.rpm", Ogsa.Telemetry.Unit.Rpm, "Demo engine speed", 0, 9000);
        _speed = _telemetry.DeclareChannel(
            "vehicle.speed", Ogsa.Telemetry.Unit.MetrePerSecond,
            "Speed over ground", 0, 30);
        _headlights = _telemetry.DeclareBooleanChannel(
            "lights.headlights", "Headlights on/off");

        _telemetry.BindChannelRaw(_rpm, () =>
            MaxRpm * (0.5 + 0.5 * Mathf.Sin((float)_t * 2f)));
        _telemetry.BindChannel(_speed, () => AngularSpeed * Radius,
            Ogsa.Telemetry.Unit.MetrePerSecond);
        _telemetry.BindBooleanChannel(_headlights, () => true);

        // pose via manual binding (no physics body needed in the demo);
        // the node converts Godot RH/-Z to OGSA-TS LH/+Z inside BindPose.
        _telemetry.BindPose(
            () => GlobalPosition,
            () => GlobalTransform.Basis.GetRotationQuaternion());
    }

    public override void _Process(double delta)
    {
        _t += delta;
        float yaw = (float)(_t * AngularSpeed);
        var pos = new Vector3(
            Radius * Mathf.Cos(yaw), 0.5f, Radius * Mathf.Sin(yaw));
        GlobalPosition = pos;
        // face the direction of travel (Godot -Z forward)
        var next = new Vector3(
            Radius * Mathf.Cos(yaw + 0.01f), 0.5f, Radius * Mathf.Sin(yaw + 0.01f));
        LookAt(next, Vector3.Up);
    }
}
