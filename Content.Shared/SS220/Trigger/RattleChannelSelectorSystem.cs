// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Implants.Components;
using Content.Shared.Popups;
using Content.Shared.Radio;
using Content.Shared.Trigger.Components.Effects;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Trigger;

/// <summary>
/// Picks the radio channel of a death rattle implant through the context menu of the implanter holding it.
/// Injecting moves the implant out of the implanter, so the verbs go away on their own and the pick
/// becomes final without needing an explicit "is it implanted yet" check.
/// </summary>
public sealed class RattleChannelSelectorSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ImplanterComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
    }

    private void OnGetVerbs(Entity<ImplanterComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!TryGetImplant(ent, out var implant))
            return;

        var user = args.User;

        // Verbs are held in a SortedSet, so these end up alphabetical by channel name regardless of
        // the order Channels happens to enumerate in.
        foreach (var channelId in implant.Comp1.Channels)
        {
            if (!_proto.TryIndex(channelId, out var channel))
                continue;

            var isCurrent = implant.Comp2.RadioChannel == channelId;

            args.Verbs.Add(new Verb
            {
                Text = channel.LocalizedName,
                Category = VerbCategory.ChannelSelect,
                Disabled = isCurrent,
                Message = isCurrent ? Loc.GetString("rattle-channel-selector-already-selected") : null,
                Act = () => SetChannel((implant.Owner, implant.Comp2), channel, ent, user),
            });
        }
    }

    private void SetChannel(
        Entity<RattleOnTriggerComponent> implant,
        RadioChannelPrototype channel,
        EntityUid implanter,
        EntityUid user)
    {
        if (implant.Comp.RadioChannel == channel.ID)
            return;

        implant.Comp.RadioChannel = channel.ID;
        Dirty(implant);

        _popup.PopupClient(Loc.GetString("rattle-channel-selector-set", ("channel", channel.LocalizedName)),
            implanter,
            user);
    }

    private bool TryGetImplant(
        Entity<ImplanterComponent> implanter,
        out Entity<RattleChannelSelectorComponent, RattleOnTriggerComponent> implant)
    {
        implant = default;

        if (implanter.Comp.ImplanterSlot.ContainerSlot?.ContainedEntity is not { } contained)
            return false;

        if (!TryComp<RattleChannelSelectorComponent>(contained, out var selector) ||
            !TryComp<RattleOnTriggerComponent>(contained, out var rattle))
            return false;

        implant = (contained, selector, rattle);
        return true;
    }
}
