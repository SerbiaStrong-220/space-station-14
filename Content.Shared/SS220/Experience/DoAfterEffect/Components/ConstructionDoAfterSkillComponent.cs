// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Stacks;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Experience.DoAfterEffect.Components;

/// <summary>
/// Provides changes in <see cref="HealingDoAfterEvent"/>
/// </summary>
[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ConstructionDoAfterSkillComponent : BaseDoAfterSkillComponent
{
    [DataField]
    public HashSet<ProtoId<StackPrototype>> ComplexMaterials = new();

    public ConstructionDoAfterSkillComponent() : base()
    {
        SkillTreeGroup = "Construction";
    }
}


