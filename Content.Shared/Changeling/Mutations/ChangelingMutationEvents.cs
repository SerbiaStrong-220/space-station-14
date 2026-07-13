// SS220 Changeling
using Content.Shared.Actions;
using Robust.Shared.Prototypes;

namespace Content.Shared.Changeling.Mutations;

/// <summary>
/// Action events for the combat and defensive changeling mutations.
/// Chemical costs live on the action event so that the authoritative system can
/// validate the target before consuming the resource.
/// </summary>
public sealed partial class ChangelingArmBladeActionEvent : InstantActionEvent
{
    [DataField]
    public float ChemicalCost = 15f;
}

public sealed partial class ChangelingBoneShardActionEvent : InstantActionEvent
{
    [DataField]
    public float ChemicalCost = 15f;
}

public sealed partial class ChangelingResonantShriekActionEvent : InstantActionEvent
{
    [DataField]
    public float ChemicalCost = 30f;

    [DataField]
    public float Radius = 6f;

    [DataField]
    public TimeSpan StunTime = TimeSpan.FromSeconds(4);
}

public sealed partial class ChangelingSwapFormsActionEvent : EntityTargetActionEvent
{
    [DataField]
    public float ChemicalCost = 40f;

    [DataField]
    public TimeSpan StunTime = TimeSpan.FromSeconds(5);
}

public sealed partial class ChangelingLastResortActionEvent : InstantActionEvent
{
    [DataField]
    public float ChemicalCost = 20f;

    [DataField]
    public EntProtoId HeadslugPrototype = "MobChangelingHeadslug";
}

public sealed partial class ChangelingLayEggActionEvent : EntityTargetActionEvent
{
    [DataField]
    public TimeSpan HatchDelay = TimeSpan.FromMinutes(4);
}

public sealed partial class ChangelingBiodegradeActionEvent : InstantActionEvent
{
    [DataField]
    public float ChemicalCost = 30f;
}

public sealed partial class ChangelingEpinephrineActionEvent : InstantActionEvent
{
    [DataField]
    public float ChemicalCost = 30f;

    [DataField]
    public TimeSpan BoostDuration = TimeSpan.FromSeconds(12);
}

public sealed partial class ChangelingFleshmendActionEvent : InstantActionEvent
{
    [DataField]
    public float ChemicalCost = 20f;

    [DataField]
    public float Healing = 50f;

    [DataField]
    public TimeSpan RepeatPenaltyWindow = TimeSpan.FromSeconds(20);
}

public sealed partial class ChangelingOrganicShieldActionEvent : InstantActionEvent
{
    [DataField]
    public float ChemicalCost = 20f;

    [DataField]
    public EntProtoId ShieldPrototype = "ChangelingOrganicShield";
}

public sealed partial class ChangelingChitinousArmorActionEvent : InstantActionEvent
{
    [DataField]
    public float ChemicalCost = 25f;
}

public sealed partial class ChangelingAnatomicPanaceaActionEvent : InstantActionEvent
{
    [DataField]
    public float ChemicalCost = 20f;
}

public sealed partial class ChangelingStrainedMusclesActionEvent : InstantActionEvent;
