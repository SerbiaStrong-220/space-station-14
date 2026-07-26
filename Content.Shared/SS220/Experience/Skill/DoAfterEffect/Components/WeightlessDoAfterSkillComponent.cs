// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Experience.DoAfterEffect.Components;

/// <summary>
/// Provides changes in <see cref="DoAfterEvent"/> while experience entity weightless
/// </summary>
[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class WeightlessDoAfterSkillComponent : BaseDoAfterSkillComponent
{
    public override ProtoId<SkillTreePrototype> SkillTreeGroup { get; set; } = "ExtravehicularActivity";

    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public bool Weightless;
}


