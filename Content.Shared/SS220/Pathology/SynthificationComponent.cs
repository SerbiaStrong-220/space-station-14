// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Silicons.Laws;
using Content.Shared.SS220.Language;
using Content.Shared.SS220.Language.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Pathology;

[RegisterComponent]
public sealed partial class SynthificationComponent : Component
{
    /// <summary>The symptom this tracks.</summary>
    [DataField]
    public ProtoId<PathologyPrototype> Pathology = "Synthification";

    /// <summary>Language granted at stage 2 and left as the only one at stage 3.</summary>
    [DataField]
    public ProtoId<LanguagePrototype> Binary = "Binary";

    /// <summary>Action that opens the laws screen.</summary>
    [DataField]
    public EntProtoId LawsAction = "ActionViewLaws";

    [ViewVariables]
    public EntityUid? LawsActionEntity;

    /// <summary>Lawset rolled.</summary>
    [ViewVariables]
    public ProtoId<SiliconLawsetPrototype>? RolledLawset;

    // Original-language snapshot, taken before the first language change, restored on cure

    [ViewVariables]
    public bool Snapshotted;

    [ViewVariables]
    public HashSet<LanguageDefinition> OriginalLanguages = new();

    [ViewVariables]
    public ProtoId<LanguagePrototype>? OriginalSelected;
}
