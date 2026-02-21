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

    [DataField]
    public Dictionary<LocId, ProtoId<LocalizedDatasetPrototype>>? LoreDatasetId;
}
