namespace Content.Server.SS220.Tarot;

[RegisterComponent]
public sealed partial class TarotCardComponent : Component
{
    [DataField]
    public EntityUid? User;

    [DataField]
    public EntityUid? Target;

    [DataField]
    public bool IsReversed;

    [DataField]
    public bool IsUsed;

    [DataField]
    public EntityUid? EntityEffect;

    [DataField]
    public bool Used;

    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(25f);
}
