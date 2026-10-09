// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.SS220.TeleportationChasm;

/// <summary>
///     Added to entities which have started falling into a chasm.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
public sealed partial class TeleportationChasmFallingComponent : Component
{
    /// <summary>
    ///     Teleporter that caused the target to start falling.
    /// </summary>
    public EntityUid? SourceTeleporter;

    /// <summary>
    ///     Time it should take for the falling animation (scaling down) to complete.
    /// </summary>
    [DataField]
    public TimeSpan FallAnimationDuration = TimeSpan.FromSeconds(1.5f);

    /// <summary>
    ///     Time between starting and completing the fall.
    /// </summary>
    [DataField]
    public TimeSpan FallDuration = TimeSpan.FromSeconds(1.8f);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan FallEndTime = TimeSpan.Zero;

    /// <summary>
    ///     Original scale of the object so it can be restored if the component is removed in the middle of the animation.
    /// </summary>
    public Vector2 OriginalScale = Vector2.Zero;

    /// <summary>
    ///     Scale that the animation should bring entities to.
    /// </summary>
    public Vector2 AnimationScale = new(0.01f, 0.01f);

    /// <summary>
    ///     Whether to delete the entity instead of teleporting it.
    /// </summary>
    public bool DeleteInsteadOfTeleport;
}
