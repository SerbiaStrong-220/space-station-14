// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Inventory.Events;
using Content.Shared.Popups;
using Content.Shared.Trigger;
using Robust.Shared.Containers;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.MartialArts;

public sealed partial class GrantMartialArtsSystem : EntitySystem
{
    [Dependency] private readonly SharedMartialArtsSystem _martialArts = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MartialArtOnTriggerComponent, TriggerEvent>(OnTrigger);
        SubscribeLocalEvent<MartialArtOnEquipComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<MartialArtOnEquipComponent, GotUnequippedEvent>(OnUnequipped);
        SubscribeLocalEvent<MartialArtOnEquipComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnTrigger(EntityUid uid, MartialArtOnTriggerComponent comp, TriggerEvent ev)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (ev.User is not { } user)
            return;

        if (!TryComp<MartialArtistComponent>(user, out var artist))
            return;

        if (!_martialArts.TryGrantMartialArt(user, comp.MartialArt, false, true, artist))
            _popup.PopupClient(Loc.GetString("martial-arts-cant-grant"), user);

        ev.Handled = true;
    }

    private void OnEquipped(EntityUid uid, MartialArtOnEquipComponent comp, GotEquippedEvent ev)
    {
        if (!_martialArts.CanHaveMartialArts(ev.Equipee))
            return;

        DebugTools.Assert(comp.Granted == false, $"Tried to give martial art on equipped event but this item already is granting martial art; UID: {uid}");

        comp.Granted = _martialArts.TryGrantMartialArt(ev.Equipee, comp.MartialArt, comp.OverrideExisting);
    }

    private void OnUnequipped(EntityUid uid, MartialArtOnEquipComponent comp, GotUnequippedEvent ev)
    {
        if (!comp.Granted)
            return;

        if (!_martialArts.CanHaveMartialArts(ev.Equipee))
            return;

        _martialArts.RevokeMartialArt(ev.Equipee);

        comp.Granted = false;
    }

    private void OnShutdown(EntityUid uid, MartialArtOnEquipComponent comp, ComponentShutdown ev)
    {
        if (!comp.Granted)
            return;

        if (!_container.TryGetContainingContainer(uid, out var container))
            return;

        DebugTools.Assert(HasComp<MartialArtistComponent>(container.Owner), $"On shutdown, entity {uid} had granted martial art but container entity ({container.Owner}) isn't martial artist");

        _martialArts.RevokeMartialArt(container.Owner);
    }
}
