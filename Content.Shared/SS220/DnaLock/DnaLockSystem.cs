// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Diagnostics.CodeAnalysis;
using Content.Shared.Administration.Managers;
using Content.Shared.Clothing.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Emag.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.Examine;
using Content.Shared.Forensics.Components;
using Content.Shared.Ghost;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.Events;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Mind;
using Content.Shared.Popups;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Content.Shared.SS220.DnaLock.Components;
using Content.Shared.Storage;
using Content.Shared.UserInterface;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Wieldable;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.DnaLock;

public sealed partial class DnaLockSystem : EntitySystem
{
    [Dependency] private SharedEntityEffectsSystem _entityEffects = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedJobSystem _job = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private EmagSystem _emag = default!;
    [Dependency] private ISharedAdminManager _admin = default!;
    [Dependency] private INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DnaLockableComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<DnaLockableComponent, EntGotInsertedIntoContainerMessage>(OnInsertedIntoContainer);
        SubscribeLocalEvent<DnaLockableComponent, ItemToggleActivateAttemptEvent>(OnActivateAttempt);
        SubscribeLocalEvent<DnaLockableComponent, UseInHandEvent>(OnUseInHand, after: [typeof(SharedWieldableSystem)], before: [typeof(BatteryWeaponFireModesSystem)]);
        SubscribeLocalEvent<DnaLockableComponent, AttemptShootEvent>(OnAttemptShoot);
        SubscribeLocalEvent<DnaLockableComponent, GetVerbsEvent<Verb>>(OnGetVerbs, after: [typeof(BatteryWeaponFireModesSystem)]);
        SubscribeLocalEvent<DnaLockableComponent, StorageInteractAttemptEvent>(OnStorageInteractAttempt);
        SubscribeLocalEvent<AttachedClothingComponent, BeingEquippedAttemptEvent>(OnAttachedEquipAttempt);
        SubscribeLocalEvent<DnaLockableComponent, ItemSlotEjectAttemptEvent>(OnItemSlotEjectAttempt);
        SubscribeLocalEvent<DnaLockableComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<RoleAddedEvent>(OnRoleAdded);
        SubscribeLocalEvent<DnaLockableComponent, GotEmaggedEvent>(OnEmagged);
        SubscribeLocalEvent<DnaLockableComponent, ActivatableUIOpenAttemptEvent>(OnActivatableUIOpenAttempt);
        SubscribeLocalEvent<DnaLockableComponent, BoundUserInterfaceMessageAttempt>(OnBoundUiMessageAttempt);
    }

    // Items spawned after the job was assigned would otherwise stay unusable for everyone.
    private void OnMapInit(Entity<DnaLockableComponent> ent, ref MapInitEvent args)
    {
        if (!_net.IsServer || ent.Comp.Mode != DnaLockMode.Roundstart || ent.Comp.RoundstartJob is not { } job)
            return;

        var query = EntityQueryEnumerator<MindComponent>();
        while (query.MoveNext(out var mindId, out var mind))
        {
            if (_job.MindHasJobWithId(mindId, job) && TryGetDna(mind.OwnedEntity, out var dna))
                ent.Comp.AllowedDna.Add(dna);
        }

        Dirty(ent);
    }

    private void OnInsertedIntoContainer(Entity<DnaLockableComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        if (!_net.IsServer || !ent.Comp.BindToFirstHolder)
            return;

        ent.Comp.BindToFirstHolder = false;

        var holder = args.Container.Owner;
        string? dna;
        while (!TryGetDna(holder, out dna))
        {
            if (!_container.TryGetContainingContainer(holder, out var container))
                return;

            holder = container.Owner;
        }

        RegisterDna(ent, holder, dna);
    }

    private void OnActivateAttempt(Entity<DnaLockableComponent> ent, ref ItemToggleActivateAttemptEvent args)
    {
        if (args.User is not { } user)
            return;

        if (!CheckAccess(ent, user, args.Silent))
            args.Cancelled = true;
    }

    private void OnUseInHand(Entity<DnaLockableComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled
            || !IsBlocked(ent, DnaLockBlockedActions.BatteryFireModeChange)
            || !HasComp<BatteryWeaponFireModesComponent>(ent))
            return;

        if (!CheckAccess(ent, args.User, silentFail: true))
            args.Handled = true;
    }

    private void OnAttemptShoot(Entity<DnaLockableComponent> ent, ref AttemptShootEvent args)
    {
        if (!CheckAccess(ent, args.User))
            args.Cancelled = true;
    }

    private void OnGetVerbs(Entity<DnaLockableComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        var user = args.User;

        if (IsBlocked(ent, DnaLockBlockedActions.BatteryFireModeChange)
            && HasComp<BatteryWeaponFireModesComponent>(ent)
            && !IsAuthorized(ent, user))
            args.Verbs.RemoveWhere(v => v.Category == VerbCategory.SelectType);

        // The server does not check range when executing a verb, so CanAccess is required here.
        if (ent.Comp.Mode != DnaLockMode.InRound
            || ent.Comp.Emagged
            || !args.CanAccess
            || !args.CanInteract
            || !TryGetDna(user, out var dna))
            return;

        if (!ent.Comp.LockActive || ent.Comp.AllowedDna.Count == 0)
        {
            args.Verbs.Add(new Verb
            {
                Text = Loc.GetString("dna-lock-verb-register"),
                Icon = new SpriteSpecifier.Texture(new("/Textures/SS220/Interface/VerbIcons/dna_lock.svg.192dpi.png")),
                Act = () => RegisterDna(ent, user, dna),
            });
        }

        if (ent.Comp.AllowedDna.Contains(dna))
        {
            args.Verbs.Add(new Verb
            {
                Text = Loc.GetString(ent.Comp.LockActive ? "dna-lock-verb-deactivate" : "dna-lock-verb-activate"),
                Icon = ent.Comp.LockActive
                    ? new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/unlock.svg.192dpi.png"))
                    : new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/lock.svg.192dpi.png")),
                Act = () => ToggleLock(ent),
            });
        }
    }

    private void OnStorageInteractAttempt(Entity<DnaLockableComponent> ent, ref StorageInteractAttemptEvent args)
    {
        if (IsBlocked(ent, DnaLockBlockedActions.StorageOpen) && !CheckAccess(ent, args.User, args.Silent))
            args.Cancelled = true;
    }

    // Checked on equip rather than on the toggle action, so deploying through the strip menu is covered too.
    private void OnAttachedEquipAttempt(Entity<AttachedClothingComponent> ent, ref BeingEquippedAttemptEvent args)
    {
        if (!TryComp<DnaLockableComponent>(ent.Comp.AttachedUid, out var dnaLock))
            return;

        var lockEnt = (ent.Comp.AttachedUid, dnaLock);
        if (!IsBlocked(lockEnt, DnaLockBlockedActions.ToggleableClothing) || IsAuthorized(lockEnt, args.EquipTarget))
            return;

        args.Reason = dnaLock.UnauthorizedUsePopup?.Id;
        args.Cancel();
    }

    private void OnItemSlotEjectAttempt(Entity<DnaLockableComponent> ent, ref ItemSlotEjectAttemptEvent args)
    {
        if (args.User is not { } user || !IsBlocked(ent, DnaLockBlockedActions.BatteryEject))
            return;

        if (!CheckAccess(ent, user, silentFail: true))
            args.Cancelled = true;
    }

    private void OnActivatableUIOpenAttempt(Entity<DnaLockableComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (IsBlocked(ent, DnaLockBlockedActions.UserInterface) && !CheckAccess(ent, args.User, args.Silent))
            args.Cancel();
    }

    private void OnBoundUiMessageAttempt(Entity<DnaLockableComponent> ent, ref BoundUserInterfaceMessageAttempt args)
    {
        if (args.Target != ent.Owner)
            return;

        var action = args.UiKey is StorageComponent.StorageUiKey.Key
            ? DnaLockBlockedActions.StorageOpen
            : DnaLockBlockedActions.UserInterface;

        if (IsBlocked(ent, action) && !CheckAccess(ent, args.Actor, silentFail: args.Message is not OpenBoundInterfaceMessage))
            args.Cancel();
    }

    private void OnExamined(Entity<DnaLockableComponent> ent, ref ExaminedEvent args)
    {
        if (!ent.Comp.ShowExamine)
            return;

        if (ent.Comp.Emagged)
        {
            args.PushMarkup(Loc.GetString("dna-lock-examine-emagged"));
            return;
        }

        if (ent.Comp.Mode == DnaLockMode.Roundstart)
        {
            args.PushMarkup(Loc.GetString(ent.Comp.AllowedDna.Count > 0
                ? "dna-lock-examine-roundstart-locked"
                : "dna-lock-examine-roundstart-waiting"));
            return;
        }

        if (ent.Comp.AllowedDna.Count == 0)
        {
            args.PushMarkup(Loc.GetString("dna-lock-examine-inround-unset"));
            return;
        }

        args.PushMarkup(Loc.GetString(ent.Comp.LockActive
            ? "dna-lock-examine-inround-active"
            : "dna-lock-examine-inround-inactive"));
    }

    private void OnRoleAdded(RoleAddedEvent ev)
    {
        if (!_net.IsServer
            || !_job.MindTryGetJobId(ev.MindId, out var jobId)
            || !TryGetDna(ev.Mind.OwnedEntity, out var dna))
            return;

        var query = EntityQueryEnumerator<DnaLockableComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Mode != DnaLockMode.Roundstart || comp.RoundstartJob != jobId)
                continue;

            if (comp.AllowedDna.Add(dna))
                Dirty(uid, comp);
        }
    }

    private void OnEmagged(Entity<DnaLockableComponent> ent, ref GotEmaggedEvent args)
    {
        if (ent.Comp.Mode != DnaLockMode.InRound
            || ent.Comp.Emagged
            || !_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;

        ent.Comp.Emagged = true;
        ent.Comp.AllowedDna.Clear();
        ent.Comp.LockActive = false;
        Dirty(ent);
        args.Handled = true;
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

    /// <summary>
    /// Checks whether <paramref name="user"/> may use the item, showing the denial feedback unless <paramref name="silentFail"/> is set.
    /// </summary>
    public bool CheckAccess(Entity<DnaLockableComponent> ent, EntityUid user, bool silentFail = false)
    {
        if (IsAuthorized(ent, user))
            return true;

        if (!silentFail)
            DenyUse(ent, user);

        return false;
    }

    private bool IsAuthorized(Entity<DnaLockableComponent> ent, EntityUid user)
    {
        if (HasComp<GhostComponent>(user) && _admin.IsAdmin(user))
            return true;

        if (ent.Comp.Emagged)
            return true;

        if (ent.Comp.Mode == DnaLockMode.InRound && !ent.Comp.LockActive)
            return true;

        return TryGetDna(user, out var dna) && ent.Comp.AllowedDna.Contains(dna);
    }

    private void DenyUse(Entity<DnaLockableComponent> ent, EntityUid user)
    {
        if (ent.Comp.UnauthorizedUsePopup is { } popupId)
            _popup.PopupClient(Loc.GetString(popupId), ent.Owner, user);

        if (ent.Comp.UnauthorizedUseSound is { } sound)
            _audio.PlayPredicted(sound, ent.Owner, user);

        foreach (var effect in ent.Comp.UnauthorizedUseEffects)
        {
            _entityEffects.ApplyEffect(user, effect);
        }
    }

    private bool TryGetDna(EntityUid? uid, [NotNullWhen(true)] out string? dna)
    {
        dna = TryComp<DnaComponent>(uid, out var comp) ? comp.DNA : null;
        return !string.IsNullOrEmpty(dna);
    }

    private static bool IsBlocked(Entity<DnaLockableComponent> ent, DnaLockBlockedActions action)
    {
        return (ent.Comp.BlockedActions & action) != 0;
    }
}
