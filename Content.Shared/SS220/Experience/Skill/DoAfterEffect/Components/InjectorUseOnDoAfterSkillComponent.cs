// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Experience.DoAfterEffect.Components;

/// <summary>
/// Provides changes in <see cref="InjectorDoAfterEvent"/>
/// </summary>
[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class InjectorUseOnDoAfterSkillComponent : BaseDoAfterSkillComponent
{
    public override ProtoId<SkillTreePrototype> SkillTreeGroup { get; set; } = "Medicine";

    [DataField]
    [AutoNetworkedField]
    public DamageSpecifier DamageOnFailure = new();
}


