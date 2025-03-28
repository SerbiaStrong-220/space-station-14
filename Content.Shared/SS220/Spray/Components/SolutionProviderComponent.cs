// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.Spray.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Spray.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true), Access(typeof(SharedSpraySystem))]
public sealed partial class SolutionProviderComponent : Component
{
    /// <summary>
    /// The solution where reagents are extracted from for the spray.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public string SolutionId = default!;

    /// <summary>
    /// The prototype that's sprayed by the spray.
    /// </summary>
    [DataField("proto")]
    public EntProtoId Prototype;
}
