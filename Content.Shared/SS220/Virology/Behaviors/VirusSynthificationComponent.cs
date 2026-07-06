// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Silicons.Laws;
using Content.Shared.SS220.Language;
using Content.Shared.SS220.Language.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Virology.Behaviors;

[RegisterComponent]
public sealed partial class VirusSynthificationComponent : Component
{
    /// <summary>Language granted.</summary>
    [DataField]
    public ProtoId<LanguagePrototype> Binary = "Binary";

    /// <summary>Action that opens laws screen.</summary>
    [DataField]
    public EntProtoId LawsAction = "ActionViewLaws";

    /// <summary>Add language this stage.</summary>
    [DataField]
    public bool AddBinary;

    /// <summary>Clear host's other languages, leaving only added(final stage).</summary>
    [DataField]
    public bool OnlyBinary;

    [ViewVariables]
    public EntityUid? LawsActionEntity;

    /// <summary>Blacklisted lawsets.</summary>
    [DataField]
    public HashSet<ProtoId<SiliconLawsetPrototype>> ExcludedLawsets = new()
    {
        "EpsilonTerminatorLawset",
        "ErtLawset",
        "XenoborgLawset",
        "Ninja",
        "Antimov",
    };

    /// <summary>Lawset rolled deterministically, so a re-grant same lawset.</summary>
    [ViewVariables]
    public ProtoId<SiliconLawsetPrototype>? RolledLawset;

    // Original-language snapshot, so we can, restore on cure.

    [ViewVariables]
    public bool Snapshotted;

    [ViewVariables]
    public HashSet<LanguageDefinition> OriginalLanguages = [];

    [ViewVariables]
    public ProtoId<LanguagePrototype>? OriginalSelected;
}
