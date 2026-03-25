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
using Content.Shared.SS220.DnaLock.Components;
using Content.Shared.Storage;
using Content.Shared.Storage.Components;
using Content.Shared.UserInterface;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Wieldable;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;

namespace Content.Shared.SS220.DnaLock;

public sealed class DnaLockSystem : EntitySystem
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

        SubscribeLocalEvent<DnaLockableComponent, ItemToggleActivateAttemptEvent>(OnActivateAttempt);
        SubscribeLocalEvent<DnaLockableComponent, UseInHandEvent>(OnUseInHand, after: [typeof(SharedWieldableSystem)]);
        SubscribeLocalEvent<DnaLockableComponent, AttemptShootEvent>(OnAttemptShoot);
        SubscribeLocalEvent<DnaLockableComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        SubscribeLocalEvent<DnaLockableComponent, GetVerbsEvent<Verb>>(OnGetVerbs, after: [typeof(BatteryWeaponFireModesSystem)]);
        SubscribeLocalEvent<DnaLockableComponent, StorageOpenAttemptEvent>(OnStorageOpenAttempt);
        SubscribeLocalEvent<DnaLockableComponent, StorageInteractAttemptEvent>(OnStorageInteractAttempt);
        SubscribeLocalEvent<DnaLockableComponent, ToggleClothingEvent>(OnToggleableClothing, before: [typeof(ToggleableClothingSystem)]);
        SubscribeLocalEvent<DnaLockableComponent, ItemSlotEjectAttemptEvent>(OnItemSlotEjectAttempt);
        SubscribeLocalEvent<DnaLockableComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<RoleAddedEvent>(OnRoleAdded);
        SubscribeLocalEvent<DnaLockableComponent, GotEmaggedEvent>(OnEmagged);
        SubscribeLocalEvent<DnaLockableComponent, ActivatableUIOpenAttemptEvent>(OnActivatableUIOpenAttempt);
        SubscribeLocalEvent<DnaLockableComponent, BoundUserInterfaceMessageAttempt>(OnBoundUiMessageAttempt);
    }

    private void OnActivateAttempt(Entity<DnaLockableComponent> ent, ref ItemToggleActivateAttemptEvent args)
    {
        if (args.User is not {} user)
            return;

        if (CheckAccess(ent, user, args.Silent))
            return;

        args.Cancelled = true;
    }

    private void OnUseInHand(Entity<DnaLockableComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (CheckAccess(ent, args.User, silentFail: true))
            return;

        args.Handled = true;
    }

    private void OnAttemptShoot(Entity<DnaLockableComponent> ent, ref AttemptShootEvent args)
    {
        if (CheckAccess(ent, args.User))
            return;

        args.Cancelled = true;
    }

    private void OnGetVerbs(Entity<DnaLockableComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (ent.Comp.Mode != DnaLockMode.InRound || ent.Comp.Emagged)
            return;

        if (!args.CanInteract)
            return;

        var user = args.User;

        // Register DNA
        if (!ent.Comp.LockActive || ent.Comp.AllowedDna.Count == 0)
        {
            if (!TryComp<DnaComponent>(user, out var dnaComp) || string.IsNullOrEmpty(dnaComp.DNA))
                return;

            args.Verbs.Add(new AlternativeVerb
            {
                Text = Loc.GetString("dna-lock-verb-register"),
                Act = () => RegisterDna(ent, user, dnaComp.DNA)
            });
        }

        // On/off lock
        if (ent.Comp.AllowedDna.Count > 0 &&
            TryComp<DnaComponent>(user, out var userDna) &&
            ent.Comp.AllowedDna.Contains(userDna.DNA ?? string.Empty))
        {
            args.Verbs.Add(new AlternativeVerb
            {
                Text = ent.Comp.LockActive
                    ? Loc.GetString("dna-lock-verb-deactivate")
                    : Loc.GetString("dna-lock-verb-activate"),
                Act = () => ToggleLock(ent)
            });
        }
    }

    private void OnGetVerbs(Entity<DnaLockableComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!ent.Comp.BlockBatteryFireModeChange)
            return;

        if (!TryComp<BatteryWeaponFireModesComponent>(ent, out _))
            return;

        if (IsAuthorized(ent, args.User))
            return;

        args.Verbs.RemoveWhere(v => v.Category == VerbCategory.SelectType);
    }

    private void OnStorageOpenAttempt(Entity<DnaLockableComponent> ent, ref StorageOpenAttemptEvent args)
    {
        if (!ent.Comp.BlockStorageOpen)
            return;

        if (CheckAccess(ent, args.User, args.Silent))
            return;

        args.Cancelled = true;
    }

    private void OnStorageInteractAttempt(Entity<DnaLockableComponent> ent, ref StorageInteractAttemptEvent args)
    {
        if (!ent.Comp.BlockStorageOpen)
            return;

        if (CheckAccess(ent, args.User, args.Silent))
            return;

        args.Cancelled = true;
    }

    private void OnToggleableClothing(Entity<DnaLockableComponent> ent, ref ToggleClothingEvent args)
    {
        if (!ent.Comp.BlockToggleableClothing)
            return;

        if (CheckAccess(ent, args.Performer))
            return;

        args.Handled = true;
    }

    private void OnItemSlotEjectAttempt(Entity<DnaLockableComponent> ent, ref ItemSlotEjectAttemptEvent args)
    {
        if (!ent.Comp.BlockBatteryEject)
            return;

        if (args.User == null)
            return;

        if (CheckAccess(ent, args.User.Value, silentFail: true))
            return;

        args.Cancelled = true;
    }

    private void OnActivatableUIOpenAttempt(Entity<DnaLockableComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (CheckAccess(ent, args.User, silentFail: true))
            return;

        args.Cancel();
    }

    private void OnBoundUiMessageAttempt(Entity<DnaLockableComponent> ent, ref BoundUserInterfaceMessageAttempt args)
    {
        if (args.Target != ent.Owner)
            return;

        var silentFail = args.Message is not OpenBoundInterfaceMessage;

        if (CheckAccess(ent, args.Actor, silentFail))
            return;

        args.Cancel();
    }

    private void OnExamined(Entity<DnaLockableComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.Emagged)
        {
            args.PushMarkup(Loc.GetString("dna-lock-examine-emagged"));
            return;
        }

        if (ent.Comp.Mode == DnaLockMode.Roundstart)
        {
            if (ent.Comp.AllowedDna.Count > 0)
                args.PushMarkup(Loc.GetString("dna-lock-examine-roundstart-locked"));
            else
                args.PushMarkup(Loc.GetString("dna-lock-examine-roundstart-waiting"));

            return;
        }

        if (ent.Comp.AllowedDna.Count == 0)
        {
            args.PushMarkup(Loc.GetString("dna-lock-examine-inround-unset"));
            return;
        }

        args.PushMarkup(ent.Comp.LockActive
            ? Loc.GetString("dna-lock-examine-inround-active")
            : Loc.GetString("dna-lock-examine-inround-inactive"));
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
        var query = EntityQueryEnumerator<DnaLockableComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Mode != DnaLockMode.Roundstart ||
                comp.RoundstartJob != jobId ||
                comp.Emagged)
            {
                continue;
            }

            comp.AllowedDna.Add(dna);
            Dirty(uid, comp);
        }
    }

    private void OnEmagged(Entity<DnaLockableComponent> ent, ref GotEmaggedEvent args)
    {
        if (ent.Comp.Mode != DnaLockMode.InRound)
            return;

        ent.Comp.Emagged = true;
        ent.Comp.AllowedDna.Clear();
        ent.Comp.LockActive = false;
        Dirty(ent);
        args.Handled = true;
        args.Repeatable = true;
    }

    public void RegisterDna(Entity<DnaLockableComponent> ent, EntityUid user, string dna)
    {
        ent.Comp.AllowedDna.Clear();
        ent.Comp.AllowedDna.Add(dna);
        ent.Comp.LockActive = true;

        Dirty(ent);
        _popup.PopupClient(Loc.GetString("dna-lock-registered-popup"), ent.Owner, user);
    }

    public void ToggleLock(Entity<DnaLockableComponent> ent)
    {
        ent.Comp.LockActive = !ent.Comp.LockActive;
        Dirty(ent);
    }

    private bool IsAuthorized(Entity<DnaLockableComponent> ent, EntityUid user)
    {
        if (HasComp<GhostComponent>(user) && _admin.IsAdmin(user))
            return true;

        if (ent.Comp.Emagged)
            return true;

        if (ent.Comp.Mode == DnaLockMode.InRound && !ent.Comp.LockActive)
            return true;

        if (ent.Comp.Mode == DnaLockMode.Roundstart && ent.Comp.AllowedDna.Count == 0)
            return false;

        if (!TryComp<DnaComponent>(user, out var dnaComp))
            return false;

        return ent.Comp.AllowedDna.Contains(dnaComp.DNA ?? string.Empty);
    }

    public bool CheckAccess(Entity<DnaLockableComponent> ent, EntityUid user, bool silentFail = false)
    {
        if (IsAuthorized(ent, user))
            return true;

        if (!silentFail)
            DenyUse(ent, user);

        return false;
    }

    private void DenyUse(Entity<DnaLockableComponent> ent, EntityUid user)
    {
        if (ent.Comp.UnauthorizedUsePopup is { } popupId)
            _popup.PopupClient(Loc.GetString(popupId), ent.Owner, user);

        if (ent.Comp.UnauthorizedUseSound is { } sound)
            _audio.PlayPredicted(sound, ent.Owner, user);

        foreach (var effect in ent.Comp.UnauthorizedUseEffects)
            _entityEffects.ApplyEffect(user, effect);
    }
}
