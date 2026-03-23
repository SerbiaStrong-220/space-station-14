using System.Linq;
using Content.Server.Radio.EntitySystems;
using Content.Server.Pinpointer;
using Content.Shared.Implants.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Radio;
using Content.Shared.Trigger;
using Content.Shared.SS220.Trigger;
using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Trigger.Systems;

public sealed class RattleOnTriggerSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly NavMapSystem _navMap = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!; // SS220 - death-rattle-implant

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RattleOnTriggerComponent, TriggerEvent>(OnTrigger);
        // SS220 - death-rattle-implant - BGN
        SubscribeLocalEvent<ImplanterComponent, BoundUIOpenedEvent>(OnBoundUiOpened);
        SubscribeLocalEvent<ImplanterComponent, RattleChannelToggledMessage>(OnChannelToggled);
        SubscribeLocalEvent<ImplanterComponent, RattleToggleAllChannelsMessage>(OnToggleAllChannels);
        // SS220 - death-rattle-implant - END
    }

    private void OnTrigger(Entity<RattleOnTriggerComponent> ent, ref TriggerEvent args)
    {
        if (args.Key != null && !ent.Comp.KeysIn.Contains(args.Key))
            return;

        var target = ent.Comp.TargetUser ? args.User : ent.Owner;

        if (target == null)
            return;

        if (!TryComp<MobStateComponent>(target.Value, out var mobstate))
            return;

        args.Handled = true;

        if (!ent.Comp.Messages.TryGetValue(mobstate.CurrentState, out var messageId))
            return;

        // Gets the location of the user
        var posText = FormattedMessage.RemoveMarkupOrThrow(_navMap.GetNearestBeaconString(target.Value));

        var message = Loc.GetString(messageId, ("user", target.Value), ("position", posText));
        // SS220 - death-rattle-implant - BGN
        foreach (var channel in ent.Comp.ActiveChannels)
        {
            if (!_prototypeManager.TryIndex(channel, out RadioChannelPrototype? channelProto))
                continue;

            _radio.SendRadioMessage(ent.Owner, message, channelProto, ent.Owner);
        }
        // SS220 - death-rattle-implant - END
    }

    // SS220 - death-rattle-implant - BGN
    private void OnBoundUiOpened(Entity<ImplanterComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (args.UiKey is not RattleUIKey.Key)
            return;

        if (!TryGetRattleImplant(ent.Comp, out var rattle))
        {
            _ui.CloseUi(ent.Owner, RattleUIKey.Key, args.Actor);
            return;
        }

        _ui.SetUiState(ent.Owner, RattleUIKey.Key, new RattleBoundUiState(GetChannelState(rattle.Comp)));
    }

    private void OnChannelToggled(Entity<ImplanterComponent> ent, ref RattleChannelToggledMessage args)
    {
        if (!TryGetRattleImplant(ent.Comp, out var rattle))
        {
            if (args.Actor.Valid)
                _ui.CloseUi(ent.Owner, RattleUIKey.Key, args.Actor);
            else
                _ui.CloseUi(ent.Owner, RattleUIKey.Key);
            return;
        }

        var allowed = rattle.Comp.AllowedChannels.ToHashSet();

        if (!allowed.Contains(args.ChannelKey))
            return;

        if (!_prototypeManager.HasIndex<RadioChannelPrototype>(args.ChannelKey))
            return;

        if (args.Enabled)
        {
            rattle.Comp.EnabledChannels.Add(args.ChannelKey);
        }
        else
        {
            rattle.Comp.EnabledChannels.Remove(args.ChannelKey);
        }

        rattle.Comp.SyncPrimaryChannel();
        Dirty(rattle);

        _ui.SetUiState(ent.Owner, RattleUIKey.Key, new RattleBoundUiState(GetChannelState(rattle.Comp)));
    }

    private void OnToggleAllChannels(Entity<ImplanterComponent> ent, ref RattleToggleAllChannelsMessage args)
    {
        if (!TryGetRattleImplant(ent.Comp, out var rattle))
        {
            if (args.Actor.Valid)
                _ui.CloseUi(ent.Owner, RattleUIKey.Key, args.Actor);
            else
                _ui.CloseUi(ent.Owner, RattleUIKey.Key);
            return;
        }

        rattle.Comp.EnabledChannels = args.Enabled
            ? rattle.Comp.AllowedChannels.ToHashSet()
            : new HashSet<ProtoId<RadioChannelPrototype>>();

        rattle.Comp.SyncPrimaryChannel();
        Dirty(rattle);

        _ui.SetUiState(ent.Owner, RattleUIKey.Key, new RattleBoundUiState(GetChannelState(rattle.Comp)));
    }

    private bool TryGetRattleImplant(ImplanterComponent implanter,
        out Entity<RattleOnTriggerComponent> rattle)
    {
        rattle = default;
        var implant = implanter.ImplanterSlot.ContainerSlot?.ContainedEntity;
        if (implant == null)
            return false;

        if (!TryComp<RattleOnTriggerComponent>(implant.Value, out var rattleComp))
            return false;

        rattle = (implant.Value, rattleComp);
        return true;
    }

    private List<RattleChannelEntry> GetChannelState(RattleOnTriggerComponent component)
    {
        var options = component.AllowedChannels;
        // UI reflects only explicit user selection; fallback default channel is runtime-only.
        var enabled = component.EnabledChannels.Select(id => id.ToString()).ToHashSet();

        var result = new List<RattleChannelEntry>();
        foreach (var channel in options)
        {
            if (!_prototypeManager.TryIndex(channel, out RadioChannelPrototype? proto))
                continue;

            result.Add(new RattleChannelEntry(proto.ID, proto.Color, Loc.GetString(proto.Name), enabled.Contains(channel)));
        }

        return result;
    }
    // SS220 - death-rattle-implant - END
}
