// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Chat.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Pathology;

[DataDefinition]
public sealed partial class SymptomManifestation
{
    /// <summary>Is examine text shown on host examination.</summary>
    [DataField]
    public bool Visible;

    /// <summary>Line added to the host's examine.</summary>
    [DataField]
    public LocId? ExamineText;

    /// <summary>Emote periodically performed by the host ( sneeze, cough). Null = no emote.</summary>
    [DataField]
    public ProtoId<EmotePrototype>? Emote;

    /// <summary>*me emote </summary>
    [DataField]
    public LocId? EmoteMessage;

    /// <summary>Feedback shown to the affected player.</summary>
    [DataField]
    public LocId? SelfMessage;

    /// <summary>Colour of feedback chat line. Null uses the default colour.</summary>
    [DataField]
    public Color? SelfMessageColor;

    /// <summary>Chance to perform emote once EmoteInterval has elapsed.</summary>
    [DataField]
    public float EmoteChance = 0.05f;

    /// <summary>Minimum time between emote rolls.</summary>
    [DataField]
    public TimeSpan EmoteInterval = TimeSpan.FromSeconds(15);
}
