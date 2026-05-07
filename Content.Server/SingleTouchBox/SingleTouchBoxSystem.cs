using Content.Server.Popups;
using Content.Server.SingleTouchBox.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Server.SingleTouchBox;

public sealed class SingleTouchBoxSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<SingleTouchBoxComponent, UseInHandEvent>(OnUseInHand);
    }

    private void OnUseInHand(
        Entity<SingleTouchBoxComponent> ent,
        ref UseInHandEvent args)
    {
        var user = args.User;
        var comp = ent.Comp;

        UpdateCombo(comp, user);

        if (TryDevour(ent, user))
        {
            return;
        }

        if (TryFirstUse(ent, user))
        {
            return;
        }

        _popup.PopupCursor(
            Loc.GetString("single-touch-box-popup-repeat"),
            user,
            PopupType.Small);
    }

    private void UpdateCombo(
        SingleTouchBoxComponent component,
        EntityUid user)
    {
        var netUser = GetNetEntity(user);

        if (component.ComboUser != netUser)
        {
            component.ComboUser = netUser;
            component.InteractionCount = 0;
        }

        component.InteractionCount++;
    }

    private bool TryFirstUse(
        Entity<SingleTouchBoxComponent> ent,
        EntityUid user)
    {
        if (!ent.Comp.UsedBy.Add(user))
            return false;

        _popup.PopupEntity(
            Loc.GetString("single-touch-box-popup-first"),
            ent.Owner,
            PopupType.Medium);

        return true;
    }

    private bool TryDevour(
        Entity<SingleTouchBoxComponent> ent,
        EntityUid user)
    {
        if (ent.Comp.InteractionCount < ent.Comp.MaxInteractionCount)
            return false;

        _hands.TryDrop(user, ent.Owner);

    _popup.PopupEntity(
        Loc.GetString("single-touch-box-popup-devour"),
        ent.Owner,
        PopupType.LargeCaution);

        _audio.PlayPvs(
            ent.Comp.DevourSound,
            ent.Owner);

        QueueDel(user);

        ent.Comp.ComboUser = null;
        ent.Comp.InteractionCount = 0;

        return true;
    }
}
