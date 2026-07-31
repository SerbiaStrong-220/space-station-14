// SS220 Changeling
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Changeling.Mutations;

public sealed partial class ChangelingApexPredatorActionEvent : InstantActionEvent; // SS220 changeling Apex tracker
public sealed partial class ChangelingAugmentedEyesightActionEvent : InstantActionEvent;
public sealed partial class ChangelingChameleonSkinActionEvent : InstantActionEvent;
public sealed partial class ChangelingContortBodyActionEvent : InstantActionEvent;
public sealed partial class ChangelingDigitalCamouflageActionEvent : InstantActionEvent;
public sealed partial class ChangelingHivemindActionEvent : InstantActionEvent;
// SS220 changeling mutations begin
public sealed partial class ChangelingDarknessAdaptationActionEvent : InstantActionEvent
{
    [DataField]
    public float ChemicalCost = 10f;

    [DataField]
    public float RegenerationModifier = 0.85f;

    [DataField]
    public float DarknessVisibility = 0.2f;

    [DataField]
    public float LightSearchRadius = 16f;

    [DataField]
    public float LightThreshold = 0.15f;

    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(0.5);
}

public sealed partial class ChangelingVoidAdaptationActionEvent : InstantActionEvent
{
    [DataField]
    public float ChemicalCost = 20f;

    [DataField]
    public float UpkeepCost = 2f;

    [DataField]
    public TimeSpan UpkeepInterval = TimeSpan.FromSeconds(2);

    [DataField]
    public float TemperatureCoefficient = 0.1f;
}
// SS220 changeling mutations end
public sealed partial class ChangelingLesserFormActionEvent : InstantActionEvent;
public sealed partial class ChangelingHumanFormActionEvent : InstantActionEvent;
public sealed partial class ChangelingMimicVoiceActionEvent : InstantActionEvent;
public sealed partial class ChangelingOrganicSpaceSuitActionEvent : InstantActionEvent;
public sealed partial class ChangelingDissonantShriekActionEvent : InstantActionEvent;
public sealed partial class ChangelingSpreadInfestationActionEvent : InstantActionEvent;

public sealed partial class ChangelingMuteStingActionEvent : EntityTargetActionEvent;
public sealed partial class ChangelingBlindStingActionEvent : EntityTargetActionEvent;
public sealed partial class ChangelingCryogenicStingActionEvent : EntityTargetActionEvent;
public sealed partial class ChangelingLethargicStingActionEvent : EntityTargetActionEvent;
public sealed partial class ChangelingFakeArmbladeStingActionEvent : EntityTargetActionEvent;

// SS220 changeling mutations begin
public sealed partial class ChangelingTransformationStingActionEvent : EntityTargetActionEvent
{
    [DataField]
    public float ChemicalCost = 40f;

    [DataField]
    public TimeSpan StingWindup = TimeSpan.FromSeconds(1.5);

    [DataField]
    public TimeSpan TransformDuration = TimeSpan.FromMinutes(1);

    [DataField]
    public float StingRange = 1.5f;
}

[Serializable, NetSerializable]
public sealed partial class ChangelingTransformationStingDoAfterEvent : SimpleDoAfterEvent;
// SS220 changeling mutations end
