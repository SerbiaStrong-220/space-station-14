// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.GameStates;
using Content.Server.SS220.CultYogg.Nyarlathotep.EntitySystems;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.SS220.CultYogg.Nyarlathotep.Components;

/// <summary>
/// A component that makes the associated entity destroy other within some distance of itself.
/// Also makes the associated entity destroy other entities upon contact.
/// Primarily managed by <see cref="SharedNyarlathotepHorizonSystem"/> and its server/client versions.
/// </summary>
[Access(friends: typeof(SharedNyarlathotepHorizonSystem))]
[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
public sealed partial class NyarlathotepHorizonComponent : Component
{
    /// <summary>
    /// The radius of the horizon within which it will destroy all entities and tiles.
    /// If &lt; 0.0 this behavior will not be active.
    /// If you want to set this go through <see cref="SharedNyarlathotepHorizonSystem.SetRadius"/>.
    /// </summary>
    [DataField("radius")]
    public float Radius;

    /// <summary>
    /// involves periodically destroying entities within a specified radius. Does not affect collide destruction of entities.
    /// </summary>
    [DataField]
    public bool ConsumeEntities = true;

    /// <summary>
    /// The ID of the fixture used to detect if the event horizon has collided with any physics objects.
    /// Can be set to null, in which case no such fixture is used.
    /// If you want to set this go through <see cref="SharedNyarlathotepHorizonSystem.SetConsumerFixtureId"/>.
    /// </summary>
    [DataField("consumerFixtureId")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string? ConsumerFixtureId;

    /// <summary>
    /// The ID of the fixture used to detect if the event horizon has collided with any physics objects.
    /// Can be set to null, in which case no such fixture is used.
    /// If you want to set this go through <see cref="SharedNyarlathotepHorizonSystem.SetColliderFixtureId"/>.
    /// </summary>
    [DataField("colliderFixtureId")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string? ColliderFixtureId;

    /// <summary>
    /// The amount of time that has to pass between this event consuming everything it intersects with.
    /// </summary>
    [DataField("consumePeriod")]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan TargetConsumePeriod = TimeSpan.FromSeconds(0.5);

    /// <summary>
    /// The next time at which this consumed everything it overlapped with.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), DataField("nextConsumeWaveTime", customTypeSerializer:typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextConsumeWaveTime;
}
