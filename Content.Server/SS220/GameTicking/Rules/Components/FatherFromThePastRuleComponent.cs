using Content.Shared.Cloning;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules.Components;

[RegisterComponent]
public sealed partial class FatherFromThePastRuleComponent : Component
{
    [DataField]
    public ProtoId<CloningSettingsPrototype> Settings = "FatherFromThePastCloningSettings";

    [DataField]
    public EntityUid? OriginalBody;

    [DataField]
    public EntityUid? OriginalMind;

    [DataField]
    public List<EntProtoId> VendingMachines = new() { "VendingMachineCigs" };

    [DataField]
    public int YearDisplacement = 15;

    [DataField]
    public EntProtoId? SpawnEffect = "EffectFlashBluespace";
}
