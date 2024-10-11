// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Audio;
using Content.Shared.FixedPoint;

namespace Content.Server.SS220.ReactiveTeleportArmor;

/// <summary>
/// Randomly teleports entity when damaged.
/// </summary>
[RegisterComponent]
public sealed partial class TeleportOnDamageComponent : Component
{
    /// <summary>
    /// Up to how far to teleport the user
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float TeleportRadius = 50f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier TeleportSound = new SoundPathSpecifier("/Audio/Effects/teleport_arrival.ogg");

    /// <summary>
    /// How much damage of any type it takes to wake this entity.
    /// </summary>
    [DataField]
    public FixedPoint2 WakeThreshold = FixedPoint2.New(4);

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float TeleportChance = .9f;

    /// <summary>
    /// Need if you want to interact with the entity that provided the TeleportOnDamageComponent.
    /// </summary>
    [ViewVariables]
    public EntityUid SavedUid;

    public bool OnCoolDown = false;

    [DataField]
    public TimeSpan CoolDownTime = TimeSpan.FromSeconds(10);
}


