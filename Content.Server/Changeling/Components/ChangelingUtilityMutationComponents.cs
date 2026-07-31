// SS220 Changeling
using Robust.Shared.Timing;
using Content.Shared.Cloning;
using Content.Shared.Speech;
using Content.Shared.SS220.Telepathy;
using Content.Shared.SS220.TTS;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Changeling.Components;

[RegisterComponent]
[AutoGenerateComponentPause]
public sealed partial class ChangelingUtilityStateComponent : Component
{
    public bool AugmentedEyesight;
    public bool AddedEyeProtection;
    public bool AddedThermalVision;
    public bool ChameleonSkin;
    // SS220 changeling mutations begin
    public bool DarknessAdaptation;
    public bool DarknessConcealmentActive;
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextDarknessUpdate;
    public TimeSpan DarknessUpdateInterval = TimeSpan.FromSeconds(0.5);
    public float DarknessVisibility = 0.2f;
    public float DarknessLightSearchRadius = 16f;
    public float DarknessLightThreshold = 0.15f;
    public bool VoidAdaptation;
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextVoidUpkeep;
    public TimeSpan VoidUpkeepInterval = TimeSpan.FromSeconds(2);
    public float VoidUpkeepCost = 2f;
    public float VoidTemperatureCoefficient = 0.1f;
    public EntityUid? PendingTransformationTarget;
    public EntityUid? PendingTransformationIdentity;
    public float PendingTransformationChemicalCost;
    public float PendingTransformationRange;
    public TimeSpan PendingTransformationWindup;
    public TimeSpan PendingTransformationDuration;
    public bool TransformationStingInProgress;
    // SS220 changeling mutations end
    public bool Contorted;
    public bool DigitalCamouflage;
    public bool OrganicSpaceSuit;
    public EntityUid? OrganicSpaceSuitVisual;
    public EntityUid? OrganicSpaceSuitHelmetVisual;
    public bool AddedStealth;
    public bool OriginalStealthCaptured;
    public bool OriginalStealthEnabled;
    public float OriginalStealthVisibility;
    public bool AddedPressureImmunity;
    public bool AddedTemperatureProtection;
    public bool AddedTelepathy;
    public bool OriginalTelepathyCaptured;
    public bool OriginalTelepathyCanSend;
    public ProtoId<TelepathyChannelPrototype>? OriginalTelepathyChannel;
    public bool OriginalTelepathyReceiveAllChannels;
    public int MimicVoiceIndex = -1;
    public bool MimicVoiceCaptured;
    public bool HadOriginalTts;
    public ProtoId<TTSVoicePrototype>? OriginalVoice;
    public bool AddedVoiceOverride;
    public bool OriginalVoiceOverrideCaptured;
    public string? OriginalVoiceOverrideName;
    public ProtoId<SpeechVerbPrototype>? OriginalSpeechVerbOverride;
    public bool OriginalVoiceOverrideEnabled;
    public string? OriginalLastSetVoice;
}

[RegisterComponent]
public sealed partial class ChangelingContortedComponent : Component
{
    /// <summary>
    /// Original collision masks for body fixtures, restored when contortion ends.
    /// </summary>
    public Dictionary<string, int> OriginalFixtureMasks = new();
}

[RegisterComponent]
public sealed partial class ChangelingOrganicSpaceSuitComponent : Component;

// SS220 changeling mutations begin
[RegisterComponent]
public sealed partial class ChangelingEnvironmentalProtectionComponent : Component
{
    [DataField]
    public float TemperatureCoefficient = 1f;

    [DataField]
    public bool RespirationImmunity;
}

/// <summary>
/// Stores the victim's original humanoid state while Transformation Sting is active.
/// The backup exists on the changeling identity paused map and is deleted after restoration.
/// </summary>
[RegisterComponent]
[AutoGenerateComponentPause]
public sealed partial class ChangelingTransformationStingComponent : Component
{
    [DataField]
    public EntityUid? Backup;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan EndTime;

    [DataField]
    public ProtoId<CloningSettingsPrototype> CloningSettings = "ChangelingCloningSettings";
}
// SS220 changeling mutations end

[RegisterComponent]
[AutoGenerateComponentPause]
public sealed partial class ChangelingCryogenicStingComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan EndTime;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextTick;

    public float DamageMultiplier = 1f;
}

/// <summary>
/// Timed blindness owned by Blind Sting. This remains separate from the legacy generic blindness status so
/// expiring the sting cannot remove temporary blindness applied by another source.
/// </summary>
[RegisterComponent]
[AutoGenerateComponentPause]
public sealed partial class ChangelingBlindStingComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan EndTime;
}
