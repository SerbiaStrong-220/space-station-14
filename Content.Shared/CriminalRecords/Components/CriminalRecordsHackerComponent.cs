using Content.Shared.CriminalRecords.Systems;
using Content.Shared.Dataset;
using Content.Shared.SS220.CriminalRecords; // SS220-criminal-console-fix
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.CriminalRecords.Components;

/// <summary>
/// Lets the user hack a criminal records console, once.
/// Everyone is set to wanted with a randomly picked reason.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedCriminalRecordsHackerSystem))]
public sealed partial class CriminalRecordsHackerComponent : Component
{
    /// <summary>
    /// How long the doafter is for hacking it.
    /// </summary>
    public TimeSpan Delay = TimeSpan.FromSeconds(20);

    /// <summary>
    /// Dataset of random reasons to use.
    /// </summary>
    [DataField]
    public ProtoId<LocalizedDatasetPrototype> Reasons = "CriminalRecordsWantedReasonPlaceholders";

    // SS220-criminal-console-fix
    /// <summary>
    /// CriminalStatus which will be after hacking
    /// </summary>
    [DataField]
    public ProtoId<CriminalStatusPrototype> CriminalStatusPrototype = "wanted";
    // SS220-criminal-console-fix

    /// <summary>
    /// Announcement made after the console is hacked.
    /// </summary>
    [DataField]
    public LocId Announcement = "ninja-criminal-records-hack-announcement";
}
