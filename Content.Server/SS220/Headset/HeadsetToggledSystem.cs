using Content.Server.Radio;
using Content.Shared.Mind;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using Content.Shared.Roles;
using Content.Shared.SS220.Headset;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Headset;

public sealed class HeadsetToggledSystem : SharedHeadsetToggledSystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedRoleSystem _role = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<HeadsetToggledComponent, ComponentInit>(OnStartup);
        SubscribeLocalEvent<HeadsetToggledComponent, EncryptionChannelsChangedEvent>(OnChangeKey);
        SubscribeLocalEvent<HeadsetToggledComponent, RadioReceiveAttemptEvent>(OnSendRadio);
        SubscribeLocalEvent<HeadsetToggledComponent, BoundUIOpenedEvent>(OnBoundOpen);
        SubscribeLocalEvent<HeadsetToggledComponent, HeadsetChannelToggledMessage>(OnToggleChannel);
    }

    private void OnStartup(Entity<HeadsetToggledComponent> ent, ref ComponentInit args)
    {
        if (!TryComp<EncryptionKeyHolderComponent>(ent.Owner, out var holderComponent))
            return;

        foreach (var channel in holderComponent.Channels)
        {
            if (!_proto.TryIndex<RadioChannelPrototype>(channel, out var channelPrototype))
                continue;

            ent.Comp.RadioChannels.Add(channelPrototype, true);
        }
    }

    private void OnChangeKey(Entity<HeadsetToggledComponent> ent, ref EncryptionChannelsChangedEvent args)
    {
        var headsetToggled = ent.Comp;
        var newChannels = args.Component.Channels;

        foreach (var channel in newChannels)
        {
            if (!_proto.TryIndex<RadioChannelPrototype>(channel, out var channelPrototype))
                continue;

            headsetToggled.RadioChannels.TryAdd(channelPrototype, true);
        }
    }

    private void OnSendRadio(Entity<HeadsetToggledComponent> ent, ref RadioReceiveAttemptEvent args)
    {
        if (!ent.Comp.RadioChannels.TryGetValue(args.Channel, out var canSend))
            return;

        if (!canSend)
            args.Cancelled = true;
    }

    private void OnBoundOpen(Entity<HeadsetToggledComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (!_mind.TryGetMind(args.Actor, out var mind, out _))
            return;

        var isAntag = _role.MindIsAntagonist(mind);

        var listChannels = new List<(string id, Color color, string name, bool enabled)>();

        foreach (var (proto, enabled) in ent.Comp.RadioChannels)
        {
            if (proto.StealthChannel is true && !isAntag)
                continue;

            var name = Loc.GetString(proto.Name);
            listChannels.Add((proto.ID, proto.Color, name, enabled));
        }

        var ev = new HeadsetSetListEvent(GetNetEntity(ent.Owner), listChannels);
        RaiseNetworkEvent(ev, args.Actor);
    }

    private void OnToggleChannel(Entity<HeadsetToggledComponent> ent, ref HeadsetChannelToggledMessage args)
    {
        if (!_mind.TryGetMind(args.Actor, out var mind, out _))
            return;

        var isAntag = _role.MindIsAntagonist(mind);

        foreach (var (proto, _) in ent.Comp.RadioChannels)
        {
            if (proto.ID != args.ChannelKey)
                continue;

            ent.Comp.RadioChannels[proto] = args.Enabled;
            break;
        }

        var filtered = new List<(string Id, Color Color, string Name, bool Enabled)>();

        foreach (var (proto, enabled) in ent.Comp.RadioChannels)
        {
            if (proto.StealthChannel is true && !isAntag)
                continue;

            filtered.Add((proto.ID, proto.Color, Loc.GetString(proto.Name), enabled));
        }

        _ui.SetUiState(ent.Owner, HeadsetKey.Key, new HeadsetBoundInterfaceState(filtered));
    }
}
