using Robust.Shared.Audio;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.GameObjects;
using Content.Server.Explosion.Components;

namespace Content.Server.SS220.ReactiveTeleportArmor;

/// <summary>
/// Randomly teleports entity when triggered.
/// </summary>
[RegisterComponent]
public sealed partial class ReactiveTeleportArmorComponent : Component
{
    /// <summary>
    /// Up to how far to teleport the user
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float TeleportRadius = 100f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier TeleportSound = new SoundPathSpecifier("/Audio/Effects/teleport_arrival.ogg");

    /// <summary>
    /// How much damage of any type it takes to wake this entity.
    /// </summary>
    [DataField]
    public FixedPoint2 WakeThreshold = FixedPoint2.New(4);

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float TeleportChance = .5f;

    [ViewVariables, AutoNetworkedField]
    public EntityUid ArmorUid;
}


public sealed partial class OnReactiveTeleportArmorEvent : Component
{

}


