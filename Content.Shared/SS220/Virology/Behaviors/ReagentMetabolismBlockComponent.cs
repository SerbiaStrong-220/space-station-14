// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Chemistry.Reagent;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Virology.Behaviors;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ReagentMetabolismBlockComponent : Component
{
    /// <summary>Reagents host can't metabolise.</summary>
    [DataField, AutoNetworkedField]
    public List<ProtoId<ReagentPrototype>> Reagents = [];
}
