namespace Content.Server.SS220.AutoEngrave;

[RegisterComponent]
public sealed partial class AutoEngravingComponent : Component
{
    [DataField]
    public string? AutoEngraveLocKey;

    [DataField(required: true)]
    public string EngravedText;
}
