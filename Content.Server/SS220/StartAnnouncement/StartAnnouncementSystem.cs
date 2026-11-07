// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Chat.Systems;
using Content.Shared.GameTicking;
using Content.Shared.SS220.StartAnnouncement;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.SS220.StartAnnouncement;

public sealed class StartAnnouncementSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private int _countLastAnnounce;
    private TimeSpan? _announcementTime;

    private readonly ProtoId<AnnouncementLorePrototype> _protoLore = "StartAnnounceLore";

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<RoundStartedEvent>(OnRoundStarted);
        SubscribeLocalEvent<PlayStartAnnouncementEvent>(OnPlayAnnounce);
        SubscribeLocalEvent<RoundEndedEvent>(OnRoundEnded);
    }

    private void OnRoundStarted(RoundStartedEvent ev)
    {
        _countLastAnnounce++;

        if (!_proto.Resolve(_protoLore, out var protoLore))
            return;

        if (_countLastAnnounce < protoLore.IdleRound)
            return;

        if (!_random.Prob(protoLore.SendChance))
            return;

        _countLastAnnounce = 0;
        _announcementTime = _timing.CurTime + protoLore.IdleTime;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_announcementTime == null || _timing.CurTime < _announcementTime)
            return;

        _announcementTime = null;

        var ev = new PlayStartAnnouncementEvent();
        RaiseLocalEvent(ev);
    }

    private void OnPlayAnnounce(PlayStartAnnouncementEvent ev)
    {
        if (!_proto.Resolve(_protoLore, out var protoLore))
            return;

        if (protoLore.LoreDatasetId == null || protoLore.LoreDatasetId.Count == 0)
            return;

        var department = _random.Pick(protoLore.LoreDatasetId.Keys);

        if (!protoLore.LoreDatasetId.TryGetValue(department, out var datasetId))
            return;

        if (!_proto.Resolve(datasetId, out var datasetPrototype))
            return;

        var currentMessage = _random.Pick(datasetPrototype.Values);


        _chat.DispatchGlobalAnnouncement(Loc.GetString(currentMessage), Loc.GetString(department), colorOverride: Color.Gold);
    }

    private void OnRoundEnded(RoundEndedEvent ev)
    {
        _announcementTime = null;
    }
}

public sealed class PlayStartAnnouncementEvent : EntityEventArgs
{

}
