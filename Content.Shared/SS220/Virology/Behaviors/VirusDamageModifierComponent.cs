// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Virology.Behaviors;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class VirusDamageModifierComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public DamageModifierSet Modifier = new();
}
