namespace Content.Server.SS220.AutoEngrave;

[RegisterComponent]
public sealed partial class EngraveNameOnOpenComponent : Component
{
    [DataField]
    public bool Activated;

    [DataField]
    public string? AutoEngraveLocKey;

    [DataField]
    public HashSet<string> ToEngrave = new();
}
