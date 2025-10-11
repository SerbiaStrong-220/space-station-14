// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;

namespace Content.Shared.SS220.CultYogg.Nyarlathotep;

/// <summary>
/// A component that makes the associated entity destroy other within some distance of itself.
/// Also makes the associated entity destroy other entities upon contact.
/// Primarily managed by <see cref="SharedNyarlathotepHorizonSystem"/> and its server/client versions.
/// </summary>
[Access(friends: typeof(SharedNyarlathotepHorizonSystem))]
[RegisterComponent, NetworkedComponent]
public sealed partial class NyarlathotepHorizonComponent : Component
{
    /// <summary>
    /// The ID of the fixture used to detect if the event horizon has collided with any physics objects.
    /// Can be set to null, in which case no such fixture is used.
    /// If you want to set this go through <see cref="SharedNyarlathotepHorizonSystem.SetColliderFixtureId"/>.
    /// </summary>
    [DataField]
    public string? ColliderFixtureId;
}
