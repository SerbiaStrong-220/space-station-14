// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Pathology;

/// <summary>
/// This component just marks entity as direct defibrillator
/// </summary>
[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PathologyHealerComponent : Component
{
    /// <summary>
    /// Used to define shat we will cure on usage.
    /// Curing is negative stack
    /// Adding is positive stack
    /// </summary>
    [DataField(required: true)]
    [AutoNetworkedField]
    public List<Dictionary<ProtoId<PathologyPrototype>, int>> CurePathologyStacksSelectors;

    [DataField, AutoNetworkedField]
    public TimeSpan Delay = TimeSpan.FromSeconds(5f);

    [DataField]
    [AutoNetworkedField]
    public DamageSpecifier DamagePerCure = new()
    {
        DamageDict = new()
        {
            { "Blunt", 5 }
        },
    };
}
