using System.Linq;
using Content.Shared.Ghost;
using Content.Shared.Radio;
using Content.Shared.SS220.GhostHearing;
using Content.Shared.SS220.Telepathy;
using Content.Shared.SS220.TTS;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.GhostHearing;

public sealed class GhostHearingSystem : SharedGhostHearingSystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    private const string Handheld = "Handheld";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GhostHearingComponent, MapInitEvent>(OnHearingStartup);
        SubscribeLocalEvent<GhostHearingComponent, BoundUIOpenedEvent>(OnBoundOpen);
        SubscribeLocalEvent<GhostHearingComponent, ToggleGhostRadioChannels>(OnToggleRadioChannelsUI);

        SubscribeLocalEvent<GhostHearingComponent, GhostHearingChannelToggledMessage>(OnToggleChannel);
        SubscribeLocalEvent<GhostHearingComponent, GhostHearingToggledAllChannelsMessage>(OnToggleAllChannel);

        SubscribeLocalEvent<GhostHearingComponent, RadioTtsSendAttemptEvent>(OnRadioAttempt);

        SubscribeLocalEvent<GhostHearingComponent, TelepathyTtsSendAttemptEvent>(OnTelepathyAttempt);
    }

    private void OnHearingStartup(Entity<GhostHearingComponent> ent, ref MapInitEvent args)
    {
        var radioProtos =
            _prototypeManager.EnumeratePrototypes<RadioChannelPrototype>()
            .Cast<IHearableChannelPrototype>();
        var telepathyProtos =
            _prototypeManager.EnumeratePrototypes<TelepathyChannelPrototype>()
            .Cast<IHearableChannelPrototype>();

        var allChannels = radioProtos.Concat(telepathyProtos);

        var seenHandheld = false;

        foreach (var proto in allChannels)
        {
            ent.Comp.RadioChannels[proto] = true;

            if (proto.ID.StartsWith(Handheld))
            {
                if (seenHandheld)
                    continue;

                ent.Comp.DisplayChannels.Add(proto, true);
                seenHandheld = true;
            }
            else
            {
                ent.Comp.DisplayChannels.Add(proto, true);
            }
        }

        Dirty(ent);
    }

    private void OnToggleChannel(Entity<GhostHearingComponent> ent, ref GhostHearingChannelToggledMessage ev)
    {
        var isHandheldGroup = ev.ChannelKey.StartsWith(Handheld);

        foreach (var proto in ent.Comp.RadioChannels.Keys.ToArray())
        {
            if (isHandheldGroup && proto.ID.StartsWith(Handheld))
            {
                ent.Comp.RadioChannels[proto] = ev.Enabled;

                if (ent.Comp.DisplayChannels.ContainsKey(proto))
                    ent.Comp.DisplayChannels[proto] = ev.Enabled;
            }
            else if (proto.ID == ev.ChannelKey)
            {
                ent.Comp.RadioChannels[proto] = ev.Enabled;
                ent.Comp.DisplayChannels[proto] = ev.Enabled;
                break;
            }
        }

        Dirty(ent);
    }

    private void OnToggleAllChannel(Entity<GhostHearingComponent> ent, ref GhostHearingToggledAllChannelsMessage args)
    {
        foreach (var proto in ent.Comp.RadioChannels.Keys.ToArray())
        {
            ent.Comp.RadioChannels[proto] = args.Enabled;
            if (ent.Comp.DisplayChannels.ContainsKey(proto))
                ent.Comp.DisplayChannels[proto] = args.Enabled;
        }

        if (!TryComp<ActorComponent>(ent.Owner, out var actor))
            return;

        var ev = new GhostHearingSetListEvent(GetNetEntity(ent.Owner), GetSortedChannelList(ent.Comp));
        RaiseNetworkEvent(ev, actor.PlayerSession);
    }

    private void OnBoundOpen(Entity<GhostHearingComponent> ent, ref BoundUIOpenedEvent args)
    {
        var ev = new GhostHearingSetListEvent(
            GetNetEntity(ent.Owner),
            GetSortedChannelList(ent.Comp));

        RaiseNetworkEvent(ev, args.Actor);
    }

    private void OnToggleRadioChannelsUI(Entity<GhostHearingComponent> ent, ref ToggleGhostRadioChannels args)
    {
        if (!TryComp<ActorComponent>(ent.Owner, out var actorComponent))
            return;

        if (!_ui.IsUiOpen(ent.Owner, GhostHearingKey.Key))
        {
            _ui.OpenUi(ent.Owner, GhostHearingKey.Key, actorComponent.PlayerSession);
            return;
        }

        _ui.CloseUi(ent.Owner, GhostHearingKey.Key);
    }

    private void OnRadioAttempt(Entity<GhostHearingComponent> ent, ref RadioTtsSendAttemptEvent args)
    {
        if (!_prototypeManager.TryIndex<RadioChannelPrototype>(args.Channel, out var channelProto))
            return;

        if (ent.Comp.RadioChannels.TryGetValue(channelProto, out var canHear) && !canHear)
            args.Cancel();
    }

    private void OnTelepathyAttempt(Entity<GhostHearingComponent> ent, ref TelepathyTtsSendAttemptEvent ev)
    {
        if (!TryComp<GhostHearingComponent>(ev.User, out var ghost))
            return;

        if (ev.Channel is null)
            return;

        if (!_prototypeManager.TryIndex<TelepathyChannelPrototype>(ev.Channel.ID, out var channelProto))
            return;

        if (ghost.RadioChannels.TryGetValue(channelProto, out var canHear) && !canHear)
            ev.Cancel();
    }

    private List<(string id, Color color, string name, bool enabled)> GetSortedChannelList(GhostHearingComponent comp)
    {
        return comp.DisplayChannels
            .Select(pair => (pair.Key.ID, pair.Key.Color, pair.Key.LocalizedName, pair.Value))
            .OrderBy(pair => pair.LocalizedName)
            .ToList();
    }
}
