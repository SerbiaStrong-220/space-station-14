using Robust.Shared.GameObjects;

namespace Content.Shared.SS220.DelayedKnockdown;

[RegisterComponent]
public sealed partial class DelayedKnockdownImmunityComponent : Component
{
    public bool Worn = true;
}
