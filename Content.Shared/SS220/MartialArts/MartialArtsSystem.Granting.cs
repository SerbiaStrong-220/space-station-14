// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Database;
using Content.Shared.Inventory.Events;
using Content.Shared.Trigger;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.MartialArts;

public sealed partial class MartialArtsSystem
{
    private void OnTrigger(Entity<MartialArtOnTriggerComponent> entity, ref TriggerEvent ev)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (ev.User is not { } user)
            return;

        if (!TryComp<MartialArtistComponent>(user, out var artist))
            return;

        if (!TryGrantMartialArt((user, artist), entity.Comp.MartialArt, false, true))
            _popup.PopupClient(Loc.GetString(artist.CantGrantArtPopup), user, user);

        ev.Handled = true;
    }

    private void OnEquipped(Entity<MartialArtOnEquipComponent> entity, ref GotEquippedEvent ev)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (!CanHaveMartialArts(ev.EquipTarget))
            return;

        DebugTools.Assert(entity.Comp.Granted == false, $"Tried to give martial art on equipped event but this item already is granting martial art; entity: {entity}");

        entity.Comp.Granted = TryGrantMartialArt(ev.EquipTarget, entity.Comp.MartialArt, entity.Comp.OverrideExisting);
    }

    private void OnUnequipped(Entity<MartialArtOnEquipComponent> entity, ref GotUnequippedEvent ev)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (!entity.Comp.Granted)
            return;

        if (!CanHaveMartialArts(ev.EquipTarget))
            return;

        RevokeMartialArt(ev.EquipTarget);

        entity.Comp.Granted = false;
    }

    private void OnEquipShutdown(Entity<MartialArtOnEquipComponent> entity, ref ComponentShutdown ev)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (!entity.Comp.Granted)
            return;

        if (!_container.TryGetContainingContainer(entity.Owner, out var container))
            return;

        DebugTools.Assert(HasComp<MartialArtistComponent>(container.Owner), $"On shutdown, entity {entity} had granted martial art but container entity ({container.Owner}) isn't martial artist");

        RevokeMartialArt(container.Owner);
    }

    public bool TryGrantMartialArt(Entity<MartialArtistComponent?> user, ProtoId<MartialArtPrototype> martialArt, bool overrideExisting = false, bool popups = true)
    {
        if (!Resolve(user.Owner, ref user.Comp))
            return false;

        if (!CanHaveMartialArts(user))
            return false;

        if (!_prototype.TryIndex(martialArt, out var proto))
            return false;

        if (user.Comp.MartialArt != null)
        {
            if (!overrideExisting)
                return false;

            RevokeMartialArt(user, popups);
        }

        user.Comp.MartialArt = martialArt;

        StartupEffects(user, proto);

        if (popups)
            _popup.PopupClient(Loc.GetString(user.Comp.GrantedArtPopup, ("art", Loc.GetString(proto.Name))), user, user);

        _adminLog.Add(LogType.Experience, LogImpact.Medium, $"{ToPrettyString(user):player} was granted with \"{proto.ID:martial art}\"");

        return true;
    }

    public void RevokeMartialArt(Entity<MartialArtistComponent?> user, bool popups = true)
    {
        if (!Resolve(user.Owner, ref user.Comp))
            return;

        if (user.Comp.MartialArt == null)
            return;

        _prototype.TryIndex(user.Comp.MartialArt, out var proto);

        user.Comp.MartialArt = null;

        if (proto != null)
            ShutdownEffects(user, proto);

        if (popups)
            _popup.PopupClient(Loc.GetString(user.Comp.RevokedArtPopup, ("art", Loc.GetString(proto?.Name ?? UnknownArt))), user, user);

        _adminLog.Add(LogType.Experience, LogImpact.Medium, $"\"{proto?.ID:martial art}\" has been revoked for {ToPrettyString(user):player}");
    }

    public bool HasMartialArt(Entity<MartialArtistComponent?> user)
    {
        if (!Resolve(user.Owner, ref user.Comp))
            return false;

        return user.Comp.MartialArt != null;
    }

    public bool CanHaveMartialArts(Entity<MartialArtistComponent?> user)
    {
        return Resolve(user.Owner, ref user.Comp, logMissing: true);
    }

}
