using Content.Shared.SS220.Tarot;

namespace Content.Server.SS220.Tarot;

[RegisterComponent]
public sealed partial class TarotCardComponent : Component
{
    [DataField]
    public TarotCardType CardType { get; set; }

    public EntityUid? EntityEffect;
    public bool IsReversed;
    public bool IsUsed;
}
