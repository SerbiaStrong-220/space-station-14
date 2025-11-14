// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.MartialArts.Conditions;
using Content.Shared.SS220.MartialArts.Effects;

namespace Content.Shared.SS220.MartialArts;

[DataDefinition]
public partial struct CombatSequence
{
    [DataField]
    public string? Name; // TODO: remove, used for debug purposes

    [DataField(required: true)]
    public List<CombatSequenceStep> Steps = new();

    [DataField(required: true)]
    public CombatSequenceEntry Entry = default!;
}

[DataDefinition]
public partial struct CombatSequenceEntry
{
    [DataField]
    public List<CombatSequenceCondition> Conditions = new();

    [DataField]
    public List<CombatSequenceEffect> Effects = new();

    [DataField]
    public List<CombatSequenceEntry> Entries = new();
}

public enum CombatSequenceStep
{
    Harm,
    Push,
    Grab,
    // Help
}
