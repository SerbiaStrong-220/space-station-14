namespace Content.Server.SS220.Tarot;

[RegisterComponent]
public sealed partial class TarotColodeComponent : Component
{
    [DataField]
    public List<string> CardsName = [];
}
