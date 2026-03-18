// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Administration.Managers;
using Content.Shared.Emag.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.Examine;
using Content.Shared.Ghost;
using Content.Shared.Interaction.Events;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Forensics.Components;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Popups;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Content.Shared.SS220.EquipmentDnaLock.Components;
using Content.Shared.SS220.SwitchableWeapon;
using Content.Shared.Storage;
using Content.Shared.Storage.Components;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;

namespace Content.Shared.SS220.EquipmentDnaLock;

public sealed class EquipmentDnaLockSystem : EntitySystem
{
    [Dependency] private readonly SharedEntityEffectsSystem _entityEffects = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedJobSystem _job = default!;
    [Dependency] private readonly ISharedAdminManager _admin = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EquipmentDnaLockComponent, ItemToggleActivateAttemptEvent>(OnActivateAttempt);
        SubscribeLocalEvent<EquipmentDnaLockComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<EquipmentDnaLockComponent, AttemptShootEvent>(OnAttemptShoot);
        SubscribeLocalEvent<EquipmentDnaLockComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        SubscribeLocalEvent<EquipmentDnaLockComponent, GetVerbsEvent<Verb>>(OnGetVerbs, after: new[] { typeof(BatteryWeaponFireModesSystem) });
        SubscribeLocalEvent<EquipmentDnaLockComponent, StorageOpenAttemptEvent>(OnStorageOpenAttempt);
        SubscribeLocalEvent<EquipmentDnaLockComponent, StorageInteractAttemptEvent>(OnStorageInteractAttempt);
        SubscribeLocalEvent<EquipmentDnaLockComponent, ToggleClothingEvent>(OnToggleableClothing, before: new[] { typeof(ToggleableClothingSystem) });
        SubscribeLocalEvent<EquipmentDnaLockComponent, ItemSlotEjectAttemptEvent>(OnItemSlotEjectAttempt);
        SubscribeLocalEvent<EquipmentDnaLockComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<RoleAddedEvent>(OnRoleAdded);
        SubscribeLocalEvent<EquipmentDnaLockComponent, GotEmaggedEvent>(OnEmagged);
    }

    private void OnActivateAttempt(Entity<EquipmentDnaLockComponent> ent, ref ItemToggleActivateAttemptEvent args)
    {
        if (args.User is not {} user)
            return;

        if (CheckUse(ent, user, args.Silent))
            return;

        args.Cancelled = true;
    }

    private void OnUseInHand(Entity<EquipmentDnaLockComponent> ent, ref UseInHandEvent args)
    {
        var comp = ent.Comp;

        if (comp.RequireSwitchableWeaponForHandUse && !HasComp<SwitchableWeaponComponent>(ent))
            return;

        if (CheckUse(ent, args.User))
            return;

        args.Handled = true;
    }

    private void OnAttemptShoot(Entity<EquipmentDnaLockComponent> ent, ref AttemptShootEvent args)
    {
        if (CheckUse(ent, args.User))
            return;

        args.Cancelled = true;
    }

    private void OnGetVerbs(Entity<EquipmentDnaLockComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        var comp = ent.Comp;

        if (comp.Mode != EquipmentDnaLockMode.InRound || comp.Emagged)
            return;

        if (!args.CanInteract)
            return;

        var user = args.User;

        // Register DNA
        if (!comp.LockActive || comp.AllowedDna.Count == 0)
        {
            if (!TryComp<DnaComponent>(user, out var dnaComp) || string.IsNullOrEmpty(dnaComp.DNA))
                return;

            args.Verbs.Add(new AlternativeVerb
            {
                Text = Loc.GetString("equipment-dna-lock-verb-register"),
                Act = () => RegisterDna(ent, user, dnaComp.DNA)
            });
        }

        // On/off lock
        if (comp.AllowedDna.Count > 0 &&
            TryComp<DnaComponent>(user, out var userDna) &&
            comp.AllowedDna.Contains(userDna.DNA ?? string.Empty))
        {
            args.Verbs.Add(new AlternativeVerb
            {
                Text = comp.LockActive
                    ? Loc.GetString("equipment-dna-lock-verb-deactivate")
                    : Loc.GetString("equipment-dna-lock-verb-activate"),
                Act = () => ToggleLock(ent)
            });
        }
    }

    private void OnGetVerbs(Entity<EquipmentDnaLockComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        var comp = ent.Comp;

        if (!comp.BlockBatteryFireModeChange)
            return;

        if (!TryComp<BatteryWeaponFireModesComponent>(ent, out _))
            return;

        if (IsAuthorized(ent, args.User))
            return;

        args.Verbs.RemoveWhere(v => v.Category == VerbCategory.SelectType);
    }

    private void OnStorageOpenAttempt(Entity<EquipmentDnaLockComponent> ent, ref StorageOpenAttemptEvent args)
    {
        var comp = ent.Comp;

        if (!comp.BlockStorageOpen)
            return;

        if (CheckUse(ent, args.User, args.Silent))
            return;

        args.Cancelled = true;
    }

    private void OnStorageInteractAttempt(Entity<EquipmentDnaLockComponent> ent, ref StorageInteractAttemptEvent args)
    {
        var comp = ent.Comp;

        if (!comp.BlockStorageOpen)
            return;

        if (CheckUse(ent, args.User, args.Silent))
            return;

        args.Cancelled = true;
    }

    private void OnToggleableClothing(Entity<EquipmentDnaLockComponent> ent, ref ToggleClothingEvent args)
    {
        var comp = ent.Comp;

        if (!comp.BlockToggleableClothing)
            return;

        if (CheckUse(ent, args.Performer))
            return;

        args.Handled = true;
    }

    private void OnItemSlotEjectAttempt(Entity<EquipmentDnaLockComponent> ent, ref ItemSlotEjectAttemptEvent args)
    {
        var comp = ent.Comp;

        if (!comp.BlockBatteryEject)
            return;

        if (args.User == null)
            return;

        if (CheckUse(ent, args.User.Value))
            return;

        args.Cancelled = true;
    }

    private void RegisterDna(Entity<EquipmentDnaLockComponent> ent, EntityUid user, string dna)
    {
        var comp = ent.Comp;

        comp.AllowedDna.Clear();
        comp.AllowedDna.Add(dna);
        comp.LockActive = true;

        Dirty(ent);
        _popup.PopupClient(Loc.GetString("equipment-dna-lock-registered-popup"), ent.Owner, user);
    }

    private void ToggleLock(Entity<EquipmentDnaLockComponent> ent)
    {
        ent.Comp.LockActive = !ent.Comp.LockActive;
        Dirty(ent);
    }

    private void OnExamined(Entity<EquipmentDnaLockComponent> ent, ref ExaminedEvent args)
    {
        var comp = ent.Comp;

        if (comp.Emagged)
        {
            args.PushMarkup(Loc.GetString("equipment-dna-lock-examine-emagged"));
            return;
        }

        if (comp.Mode == EquipmentDnaLockMode.Roundstart)
        {
            if (comp.AllowedDna.Count > 0)
                args.PushMarkup(Loc.GetString("equipment-dna-lock-examine-roundstart-locked"));
            else
                args.PushMarkup(Loc.GetString("equipment-dna-lock-examine-roundstart-waiting"));

            return;
        }

        if (comp.AllowedDna.Count == 0)
        {
            args.PushMarkup(Loc.GetString("equipment-dna-lock-examine-inround-unset"));
            return;
        }

        args.PushMarkup(comp.LockActive
            ? Loc.GetString("equipment-dna-lock-examine-inround-active")
            : Loc.GetString("equipment-dna-lock-examine-inround-inactive"));
    }

    private void OnRoleAdded(RoleAddedEvent ev)
    {
        if (!_net.IsServer)
            return;

        if (ev.Mind.OwnedEntity is not { } owner)
            return;

        if (!_job.MindTryGetJobId(ev.MindId, out var jobId))
            return;

        if (!TryComp<DnaComponent>(owner, out var dnaComp) || string.IsNullOrEmpty(dnaComp.DNA))
            return;

        var dna = dnaComp.DNA;
        var query = EntityQueryEnumerator<EquipmentDnaLockComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Mode != EquipmentDnaLockMode.Roundstart ||
                comp.RoundstartJob != jobId ||
                comp.Emagged)
            {
                continue;
            }

            comp.AllowedDna.Add(dna);
            Dirty(uid, comp);
        }
    }

    private void OnEmagged(Entity<EquipmentDnaLockComponent> ent, ref GotEmaggedEvent args)
    {
        if (ent.Comp.Mode != EquipmentDnaLockMode.InRound)
            return;

        ent.Comp.Emagged = true;
        ent.Comp.AllowedDna.Clear();
        ent.Comp.LockActive = false;
        Dirty(ent);
        args.Handled = true;
        args.Repeatable = true;
    }

    private bool IsAuthorized(Entity<EquipmentDnaLockComponent> ent, EntityUid user)
    {
        var comp = ent.Comp;

        if (HasComp<GhostComponent>(user) && _admin.IsAdmin(user))
            return true;

        if (comp.Emagged)
            return true;

        if (comp.Mode == EquipmentDnaLockMode.InRound && !comp.LockActive)
            return true;

        if (comp.Mode == EquipmentDnaLockMode.Roundstart && comp.AllowedDna.Count == 0)
            return false;

        if (!TryComp<DnaComponent>(user, out var dnaComp))
            return false;

        return comp.AllowedDna.Contains(dnaComp.DNA ?? string.Empty);
    }

    private bool CheckUse(Entity<EquipmentDnaLockComponent> ent, EntityUid user, bool silent = false)
    {
        if (IsAuthorized(ent, user))
            return true;

        if (!silent)
            DenyUse(ent, user);

        return false;
    }

    private void DenyUse(Entity<EquipmentDnaLockComponent> ent, EntityUid user)
    {
        var comp = ent.Comp;

        if (comp.UnauthorizedUsePopup is { } popupId)
            _popup.PopupClient(Loc.GetString(popupId), ent.Owner, user);

        if (comp.UnauthorizedUseSound is { } sound)
            _audio.PlayPredicted(sound, ent.Owner, user);

        foreach (var effect in comp.UnauthorizedUseEffects)
            _entityEffects.ApplyEffect(user, effect);
    }
}
