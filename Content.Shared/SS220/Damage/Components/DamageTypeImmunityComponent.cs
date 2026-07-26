// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Damage.Components;

/// <summary>
/// Completely removes the damage types from DamageSpecifier before
/// ignoreResistances is triggered.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DamageTypeImmunityComponent : Component
{
    [DataField(required: true)]
    public List<ProtoId<DamageTypePrototype>> ImmuneTypes = [];
}

