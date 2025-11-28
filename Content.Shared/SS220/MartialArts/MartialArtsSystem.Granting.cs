// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Database;
using Content.Shared.Inventory.Events;
using Content.Shared.Trigger;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.MartialArts;

public sealed partial class MartialArtsSystem
{
    private void OnTrigger(EntityUid uid, MartialArtOnTriggerComponent comp, TriggerEvent ev)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (ev.User is not { } user)
            return;

        if (!TryComp<MartialArtistComponent>(user, out var artist))
            return;

        if (!TryGrantMartialArt(user, comp.MartialArt, false, true, artist))
            _popup.PopupClient(Loc.GetString("martial-arts-cant-grant"), user, user);

        ev.Handled = true;
    }

    private void OnEquipped(EntityUid uid, MartialArtOnEquipComponent comp, GotEquippedEvent ev)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (!CanHaveMartialArts(ev.Equipee))
            return;

        DebugTools.Assert(comp.Granted == false, $"Tried to give martial art on equipped event but this item already is granting martial art; UID: {uid}");

        comp.Granted = TryGrantMartialArt(ev.Equipee, comp.MartialArt, comp.OverrideExisting);
    }

    private void OnUnequipped(EntityUid uid, MartialArtOnEquipComponent comp, GotUnequippedEvent ev)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (!comp.Granted)
            return;

        if (!CanHaveMartialArts(ev.Equipee))
            return;

        RevokeMartialArt(ev.Equipee);

        comp.Granted = false;
    }

    private void OnShutdown(EntityUid uid, MartialArtOnEquipComponent comp, ComponentShutdown ev)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (!comp.Granted)
            return;

        if (!_container.TryGetContainingContainer(uid, out var container))
            return;

        DebugTools.Assert(HasComp<MartialArtistComponent>(container.Owner), $"On shutdown, entity {uid} had granted martial art but container entity ({container.Owner}) isn't martial artist");

        RevokeMartialArt(container.Owner);
    }

    public bool TryGrantMartialArt(EntityUid user, ProtoId<MartialArtPrototype> martialArt, bool overrideExisting = false, bool popups = true, MartialArtistComponent? artist = null)
    {
        if (!Resolve(user, ref artist))
            return false;

        if (!CanHaveMartialArts(user))
            return false;

        if (!_prototype.TryIndex(martialArt, out var proto))
            return false;

        if (artist.MartialArt != null)
        {
            if (!overrideExisting)
                return false;

            RevokeMartialArt(user, popups, artist);
        }

        artist.MartialArt = martialArt;

        StartupEffects(user, proto);

        if (popups)
            _popup.PopupClient(Loc.GetString("martial-arts-granted-art", ("art", Loc.GetString(proto.Name))), user, user);

        _adminLog.Add(LogType.Experience, LogImpact.Medium, $"{ToPrettyString(user):player} was granted with \"{proto.ID:martial art}\"");

        return true;
    }

    public void RevokeMartialArt(EntityUid user, bool popups = true, MartialArtistComponent? artist = null)
    {
        if (!Resolve(user, ref artist))
            return;

        if (artist.MartialArt == null)
            return;

        _prototype.TryIndex(artist.MartialArt, out var proto);

        artist.MartialArt = null;

        if (proto != null)
            ShutdownEffects(user, proto);

        if (popups)
            _popup.PopupClient(Loc.GetString("martial-arts-revoked-art", ("art", Loc.GetString(proto?.Name ?? "martial-arts-unknown"))), user, user);

        _adminLog.Add(LogType.Experience, LogImpact.Medium, $"\"{proto?.ID:martial art}\" has been revoked for {ToPrettyString(user):player}");
    }

    public bool HasMartialArt(EntityUid user, MartialArtistComponent? artist = null)
    {
        if (!Resolve(user, ref artist))
            return false;

        return artist.MartialArt != null;
    }

    public bool CanHaveMartialArts(EntityUid user, MartialArtistComponent? artist = null)
    {
        if (!Resolve(user, ref artist))
            return false;

        return true;
    }

}
