// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Chemistry.Reagent;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Pathology;

/// <summary>
/// Marks host as unable to metabolise the listed reagents.
/// Let them just sit inert in the bloodstream - its funni(y)er
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ReagentMetabolismBlockComponent : Component
{
    /// <summary>Reagents the host can no longer metabolise.</summary>
    [DataField, AutoNetworkedField]
    public List<ProtoId<ReagentPrototype>> Reagents = new();
}
