using System.Linq;
using Content.Server.Chat.Systems;
using Content.Shared.GameTicking;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.SS220.StartAnnouncement;

public sealed class StartAnnouncement : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    private int _previousAnnouncement;

    private readonly ProtoId<AnnouncementLorePrototype> _protoLore = "StartAnnounceLore";

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<RoundStartedEvent>(OnRoundStarted);
    }

    private void OnRoundStarted(RoundStartedEvent ev)
    {
        var protoLore = _proto.Index<AnnouncementLorePrototype>(_protoLore);

        if (ev.RoundId - _previousAnnouncement <= protoLore.IdleRound)
            return;

        if (!_random.Prob(protoLore.SendChance))
            return;

        if (protoLore.LoreDatasetId == null || protoLore.LoreDatasetId.Count == 0)
            return;

        var department = _random.Pick(protoLore.LoreDatasetId.Keys.ToList());

        if (!protoLore.LoreDatasetId.TryGetValue(department, out var datasetId))
            return;

        if (!_proto.TryIndex(datasetId, out var datasetPrototype))
            return;

        var currentMessage = _random.Pick(datasetPrototype.Values);
        _previousAnnouncement = ev.RoundId;

        _chat.DispatchGlobalAnnouncement(Loc.GetString(currentMessage), Loc.GetString(department), colorOverride: Color.Gold);
    }
}
