// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.SS220.HolyAmulet;

/// <summary>
/// Marks holy amulet that detects nearby unholy mobs
/// and starts glowing and shaking when one is detected.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(fieldDeltas: true), AutoGenerateComponentPause]
[Access(typeof(SharedHolyAmuletSystem))]
public sealed partial class HolyAmuletComponent : Component
{
    /// <summary>
    /// Whitelist of components that identify entity unholy.
    /// </summary>
    [DataField]
    public EntityWhitelist DetectWhitelist = new();

    /// <summary>
    /// Maximum distance which unholy creatures are detected.
    /// </summary>
    [DataField]
    public float Range = 5f;

    /// <summary>
    /// Interval between detection checks.
    /// </summary>
    [DataField]
    public TimeSpan UpdateCooldown = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Next game time which detection check runs.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextUpdate = TimeSpan.Zero;

    /// <summary>
    /// Whether unholy is currently detected nearby
    /// controls the point light and sprite shake.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public bool Active;

    /// <summary>
    /// Maximum sprite rotation applied shake.
    /// </summary>
    [DataField]
    public Angle ShakeAngle = Angle.FromDegrees(5);

    /// <summary>
    /// Speed of the sprite shake.
    /// </summary>
    [DataField]
    public float ShakeSpeed = 20f;
}

