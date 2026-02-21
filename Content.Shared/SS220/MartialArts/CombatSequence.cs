// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.MartialArts.Sequence.Conditions;
using Content.Shared.SS220.MartialArts.Sequence.Effects;

namespace Content.Shared.SS220.MartialArts;

[DataDefinition]
public partial struct CombatSequence
{
    [DataField]
    public LocId Name;

    [DataField(required: true)]
    public List<CombatSequenceStep> Steps = new();

    [DataField(required: true)]
    public CombatSequenceEntry Entry = default!;

    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(2);
}

[DataDefinition]
public partial struct CombatSequenceEntry
{
    [DataField]
    public CombatSequenceCondition[] Conditions = [];

    [DataField]
    public CombatSequenceEffect[] Effects = [];

    [DataField]
    public CombatSequenceEntry[] Entries = [];
}

public enum CombatSequenceStep
{
    Harm,
    Push,
    Grab,
    // Help
}
