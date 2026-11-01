using Content.Shared.Actions;

namespace Content.Shared.SS220.DarkForces.Narsi.Abilities.Events;

public abstract partial class NarsiCultistBaseInstantEvent : InstantActionEvent, INarsiCultistAbility
{
    [DataField("speech")]
    public string? Speech { get; set; }
}

public abstract partial class NarsiCultistBaseTargetEvent : EntityTargetActionEvent, INarsiCultistAbility
{
    [DataField("speech")]
    public string? Speech { get; set; }
}

/*
 * Instant
 */

//Leader
public sealed partial class NarsiCultistLeaderEvent : NarsiCultistBaseInstantEvent
{
}

public sealed partial class NarsiCultistEmpEvent : NarsiCultistBaseInstantEvent
{
}

public sealed partial class NarsiCultistFireArmsEvent : NarsiCultistBaseInstantEvent
{
}

public sealed partial class NarsiCultistInvisibilityEvent : NarsiCultistBaseInstantEvent
{
}

public sealed partial class NarsiCultistShadowEvent : NarsiCultistBaseInstantEvent
{
}

public sealed partial class NarsiCultistTeleportEvent : NarsiCultistBaseInstantEvent
{
}

public sealed partial class NarsiCultistGhostWeaponEvent : NarsiCultistBaseInstantEvent
{
}
/*
 * Target
 */

public sealed partial class NarsiCultistSilenceEvent : NarsiCultistBaseTargetEvent
{
}

public sealed partial class NarsiCultistHealthStealEvent : NarsiCultistBaseTargetEvent
{
}

public sealed partial class NarsiCultistBlindnessEvent : NarsiCultistBaseTargetEvent
{
}

public sealed partial class NarsiCultistStunEvent : NarsiCultistBaseTargetEvent
{
}

public sealed partial class NarsiCultistCuffEvent : NarsiCultistBaseTargetEvent
{
}
