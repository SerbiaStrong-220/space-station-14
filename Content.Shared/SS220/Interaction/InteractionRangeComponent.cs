namespace Content.Shared.SS220.Interaction;

[RegisterComponent]
public sealed class InteractionRangeComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("range")]
    public float Range = 1.5f;
}
