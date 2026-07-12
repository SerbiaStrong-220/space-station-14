using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.StationEvents.Components;

// SS220-event-director-begin
/// <summary>
/// The amount of pressure a random station event puts on the round.
/// Used by the station event director to alternate between quiet and dangerous beats.
/// </summary>
public enum StationEventSeverity : byte
{
    Calm,
    Incident,
    Crisis,
}
// SS220-event-director-end

/// <summary>
///     Defines basic data for a station event
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class StationEventComponent : Component
{
    public const float WeightVeryLow = 0.0f;
    public const float WeightLow = 5.0f;
    public const float WeightNormal = 10.0f;
    public const float WeightHigh = 15.0f;
    public const float WeightVeryHigh = 20.0f;

    // SS220-more-robust-TTS-announcements-begin
    [DataField]
    public bool PlayTTS = true;
    // SS220-more-robust-TTS-announcements-end

    [DataField]
    public float Weight = WeightNormal;

    // SS220-event-director-begin
    /// <summary>
    /// Director category for a random event. Events default to calm so unclassified content
    /// cannot unexpectedly bypass the director's crisis cooldown.
    /// </summary>
    [DataField]
    public StationEventSeverity DirectorSeverity = StationEventSeverity.Calm;
    // SS220-event-director-end

    [DataField]
    public string? StartAnnouncement;

    [DataField]
    public string? EndAnnouncement;

    [DataField]
    public Color StartAnnouncementColor = Color.Gold;

    [DataField]
    public Color EndAnnouncementColor = Color.Gold;

    [DataField]
    public SoundSpecifier? StartAudio;

    [DataField]
    public SoundSpecifier? EndAudio;

    /// <summary>
    ///     In minutes, when is the first round time this event can start
    /// </summary>
    [DataField]
    public int EarliestStart = 5;

    /// <summary>
    ///     In minutes, the amount of time before the same event can occur again
    /// </summary>
    [DataField]
    public int ReoccurrenceDelay = 30;

    /// <summary>
    ///     How long the event lasts.
    /// </summary>
    [DataField]
    public TimeSpan? Duration = TimeSpan.FromSeconds(1);

    /// <summary>
    ///     The max amount of time the event lasts.
    /// </summary>
    [DataField]
    public TimeSpan? MaxDuration;

    /// <summary>
    ///     How many players need to be present on station for the event to run
    /// </summary>
    /// <remarks>
    ///     To avoid running deadly events with low-pop
    /// </remarks>
    [DataField]
    public int MinimumPlayers;

    /// <summary>
    ///     How many times this even can occur in a single round
    /// </summary>
    [DataField]
    public int? MaxOccurrences;

    /// <summary>
    /// When the station event ends.
    /// </summary>
    [DataField("endTime", customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan? EndTime;

    /// <summary>
    /// If false, the event won't trigger during ongoing evacuation.
    /// </summary>
    [DataField]
    public bool OccursDuringRoundEnd = true;

    //SS220 GiftsGamma event fix begin
    /// <summary>
    /// If true, the event isn't triggered randomly in round
    /// </summary>
    [DataField]
    public bool CannotStartRandomly = false;
    //SS220 GiftsGamma event fix end
}
