// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Dataset;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.StartAnnouncement;

[Prototype]
public sealed partial class AnnouncementLorePrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; } = default!;

    /// <summary>
    /// Number of rounds between announcements.
    /// </summary>
    [DataField]
    public int IdleRound;

    [DataField]
    public float SendChance;

    /// <summary>
    /// Time after roundstart for the announcement to be sent
    /// </summary>
    [DataField]
    public TimeSpan IdleTime;

    [DataField]
    public Dictionary<LocId, ProtoId<LocalizedDatasetPrototype>>? LoreDatasetId;
}
