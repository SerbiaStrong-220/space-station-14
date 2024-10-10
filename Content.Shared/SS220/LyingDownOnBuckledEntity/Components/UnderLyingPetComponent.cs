// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.LyingDownOnBuckledEntity.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class UnderLyingPetComponent : Component
{
    /// <summary>
    /// The uid of the pet which lies on the entity
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? PetUid;

    /// <summary>
    /// Is lying pet blocks unbuckle of the entity
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public bool BlockUnbuckle = false;

    /// <summary>
    /// Damage caused by lying pet
    /// If null - doesn't cause damage
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public DamageSpecifier? Damage;

    [ViewVariables, AutoNetworkedField]
    public TimeSpan NextSecond = TimeSpan.Zero;
}
