// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Metabolism;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Bioware;

/// <summary>
/// Bioware of the circulation group: a denser capillary network metabolizes the reagents in the blood faster.
/// Works through any heart in the body, so a transplanted heart gets it too.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(BiowareSystem))]
public sealed partial class CapillaryWeavingComponent : Component
{
    /// <summary>
    /// How many times faster the organs of the <see cref="Stage"/> metabolize.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float RateMultiplier = 1.5f;

    [DataField]
    public ProtoId<MetabolismStagePrototype> Stage = "Bloodstream";
}
