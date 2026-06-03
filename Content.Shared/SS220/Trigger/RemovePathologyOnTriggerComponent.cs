// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Pathology;
using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Trigger;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RemovePathologyOnTriggerComponent : BaseXOnTriggerComponent
{
    /// <summary>
    /// Used to define shat we will cure on usage.
    /// Curing is negative stack
    /// Adding is positive stack
    /// </summary>
    [DataField(required: true)]
    [AutoNetworkedField]
    public List<Dictionary<ProtoId<PathologyPrototype>, int>> CurePathologyStacksSelectors;
}
