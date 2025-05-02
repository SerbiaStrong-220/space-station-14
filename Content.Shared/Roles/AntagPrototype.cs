using Content.Shared.Guidebook;
using Content.Shared.Players.PlayTimeTracking;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Roles;

/// <summary>
///     Describes information for a single antag.
/// </summary>
[Prototype]
[Serializable, NetSerializable]
public sealed partial class AntagPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    ///     The name of this antag as displayed to players.
    /// </summary>
    [DataField("name")]
    public string Name { get; private set; } = "";

    /// <summary>
    ///     The antag's objective, shown in a tooltip in the antag preference menu or as a ghost role description.
    /// </summary>
    [DataField("objective", required: true)]
    public string Objective { get; private set; } = "";

    /// <summary>
    ///     Whether or not the antag role is one of the bad guys.
    /// </summary>
    [DataField("antagonist")]
    public bool Antagonist { get; private set; }

    /// <summary>
    ///     Whether or not the player can set the antag role in antag preferences.
    /// </summary>
    [DataField("setPreference")]
    public bool SetPreference { get; private set; }

    //SS220 Add antags playtime trackers begin
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<PlayTimeTrackerPrototype>))]
    public string? PlayTimeTracker { get; private set; }

    /// <summary>
    /// A color representing this antag to use for text.
    /// </summary>
    [DataField]
    public Color AntagColor = Color.Red;
    //SS220 Add antags playtime trackers end
    // SS220 Round End Titles begin
    /// <summary>
    /// Optional color that UI may use to make role label readable on dark background.
    /// </summary>
    [DataField]
    public Color? LightAntagColor;
    // SS220 Round End Titles end

    /// <summary>
    ///     Requirements that must be met to opt in to this antag role.
    /// </summary>
    // TODO ROLE TIMERS
    // Actually check if the requirements are met. Because apparently this is actually unused.
    [DataField, Access(typeof(SharedRoleSystem), Other = AccessPermissions.None)]
    public HashSet<JobRequirement>? Requirements;

    /// <summary>
    /// Optional list of guides associated with this antag. If the guides are opened, the first entry in this list
    /// will be used to select the currently selected guidebook.
    /// </summary>
    [DataField]
    public List<ProtoId<GuideEntryPrototype>>? Guides;
}
