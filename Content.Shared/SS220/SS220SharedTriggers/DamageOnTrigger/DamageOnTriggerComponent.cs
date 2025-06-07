// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage;

namespace Content.Shared.SS220.SS220SharedTriggers.DamageOnTrigger;

/// <summary>
/// This handles deals damage when triggered
/// </summary>
[RegisterComponent]
public sealed partial class DamageOnTriggerComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public DamageSpecifier? Damage;
}
