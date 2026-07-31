// SS220 Changeling
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Changeling.Mutations;

/// <summary>
/// Runtime state shared by the combat and defensive changeling mutations.
/// Purchase ownership is intentionally not stored here; it belongs to the
/// evolution store and is reset by its owning system.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true), AutoGenerateComponentPause]
public sealed partial class ChangelingMutationStateComponent : Component
{
    [DataField]
    public EntityUid? ArmBlade;

    [DataField]
    public EntityUid? ArmBladeAction;

    [DataField]
    public EntityUid? OrganicShield;

    [DataField]
    public EntityUid? OrganicShieldAction;

    [DataField]
    public EntityUid? ChitinousArmorVisual;

    [DataField]
    public EntityUid? ChitinousHelmetVisual;

    /// <summary>
    /// Server-only deadline for switching the one-shot chitinous armor formation animation to its static sprite.
    /// </summary>
    public TimeSpan? ChitinousArmorAnimationEndsAt;

    [DataField, AutoNetworkedField]
    public bool ChitinousArmorActive;

    [DataField, AutoNetworkedField]
    public bool StrainedMusclesActive;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextArmBladeDrain;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextStrainedMusclesTick;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan? EpinephrineEndsAt;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan LastFleshmend;

    public override bool SendOnlyToOwner => true;
}

/// <summary>
/// Marks the temporary shield and tracks its finite number of successful blocks.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ChangelingOrganicShieldComponent : Component
{
    [DataField, AutoNetworkedField]
    public int BlocksRemaining = 6;

    [DataField]
    public EntityUid? ChangelingOwner;
}

/// <summary>
/// Marks a changeling whose image is replaced with static in station-AI camera vision.
/// </summary>
[RegisterComponent]
public sealed partial class ChangelingDigitalCamouflageComponent : Component;

/// <summary>
/// Owner-only list of digitally camouflaged entities visible to the station AI overlay.
/// This component is attached to the station AI, never to the changeling.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class StationAiDigitalCamouflageComponent : Component
{
    [AutoNetworkedField]
    public HashSet<NetEntity> CamouflagedEntities = [];

    /// <summary>
    /// Server-side refresh deadline. The transmitted entity set must follow the AI's moving eye and camera coverage.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextRefresh;

    public override bool SendOnlyToOwner => true;
}

/// <summary>
/// Marker for the vulnerable headslug produced by Last Resort.
/// </summary>
[RegisterComponent]
public sealed partial class ChangelingHeadslugComponent : Component
{
    [DataField]
    public EntityUid? AbandonedBody;

    /// <summary>
    /// Nullspace entity that owns the changeling's persistent state while the headslug is vulnerable.
    /// The state is deliberately not installed on the headslug, so no normal changeling ability is usable
    /// before the egg hatches.
    /// </summary>
    [DataField]
    public EntityUid? StoredState;

    [DataField]
    public bool HasLaidEgg;
}

/// <summary>
/// Marks the inaccessible nullspace entity used to preserve a Last Resort changeling until hatching.
/// </summary>
[RegisterComponent]
public sealed partial class ChangelingLastResortStorageComponent : Component;

/// <summary>
/// Marks the temporary polymorph child used by Lesser Form. Persistent changeling interactions must treat this
/// body as part of its paused changeling parent without networking that hidden relationship to other clients.
/// </summary>
[RegisterComponent]
public sealed partial class ChangelingLesserFormComponent : Component;

/// <summary>
/// Incubation state attached to the corpse selected by a headslug.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class ChangelingIncubatingEggComponent : Component
{
    [DataField(required: true)]
    public EntityUid Headslug;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan HatchAt;

}
