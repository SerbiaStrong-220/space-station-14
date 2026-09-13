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
/// Injecting moves the implant out of the implanter, so the verbs go away too
/// </summary>
public sealed partial class RattleChannelSelectorSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ImplanterComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
    }

    private void OnGetVerbs(Entity<ImplanterComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (ent.Comp.ImplanterSlot.ContainerSlot?.ContainedEntity is not { } contained)
            return;

        if (!TryComp<RattleChannelSelectorComponent>(contained, out var selector)
            || !TryComp<RattleOnTriggerComponent>(contained, out var rattle))
            return;

        var user = args.User;
        foreach (var channelId in selector.Channels)
        {
            if (!_prototype.TryIndex(channelId, out var channel))
                continue;

            var selected = rattle.RadioChannel == channelId;

            args.Verbs.Add(new Verb
            {
                Text = channel.LocalizedName,
                Category = VerbCategory.ChannelSelect,
                Disabled = selected,
                Message = selected ? Loc.GetString("rattle-channel-selector-already-selected") : null,
                Act = () => SetChannel((contained, rattle), channel, ent, user),
            });
        }
    }

    private void SetChannel(Entity<RattleOnTriggerComponent> implant, RadioChannelPrototype channel, EntityUid implanter, EntityUid user)
    {
        if (implant.Comp.RadioChannel == channel.ID)
            return;

        implant.Comp.RadioChannel = channel.ID;
        Dirty(implant);

        _popup.PopupClient(Loc.GetString("rattle-channel-selector-set", ("channel", channel.LocalizedName)), implanter, user);
    }
}
