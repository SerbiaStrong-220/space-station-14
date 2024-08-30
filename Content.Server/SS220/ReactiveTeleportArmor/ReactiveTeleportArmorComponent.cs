using Robust.Shared.Audio;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.GameObjects;

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

    [ViewVariables, AutoNetworkedField]
    public EntityUid? ArmorEntity;
}


public sealed partial class OnReactiveTeleportArmorEvent : Component
{

}


