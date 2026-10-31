namespace Content.Shared.SS220.IgnoreStunEffect;

[RegisterComponent]
public sealed partial class IgnoreStunEffectComponent : Component
{
    [DataField(required: true)]
    public HashSet<string> RequiredEffects = [];

    [DataField]
    public float? Time;

    public HashSet<string> OriginalEffects = [];
}
