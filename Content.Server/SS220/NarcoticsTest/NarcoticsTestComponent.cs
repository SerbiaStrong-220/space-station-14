namespace Content.Server.SS220.NarcoticsTest;

[RegisterComponent]
public sealed partial class NarcoticsTestComponent : Component
{
    [DataField]
    public float Delay = 3f;

    [DataField]
    public bool IsUsed;

    [DataField]
    public bool IsPositive;
}
