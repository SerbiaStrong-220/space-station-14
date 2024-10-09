// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Administration.Managers;
using Content.Shared.Damage;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Mind;
using Content.Shared.Popups;
using Content.Shared.Pulling.Events;
using Content.Shared.Roles;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.RoleItem;

public abstract class SharedRoleItemSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedRoleSystem _role = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoleItemComponent, BeingEquippedAttemptEvent>(OnEquipAttempt);
        SubscribeLocalEvent<RoleItemComponent, BeingPulledAttemptEvent>(OnPullAttempt);
        SubscribeLocalEvent<RoleItemComponent, GettingPickedUpAttemptEvent>(OnPickupAttempt);
    }

    private void OnPickupAttempt(Entity<RoleItemComponent> ent, ref GettingPickedUpAttemptEvent args)
    {
        if (!ItemCheck(args.User, ent))
            args.Cancel();
    }

    private void OnPullAttempt(Entity<RoleItemComponent> ent, ref BeingPulledAttemptEvent args)
    {
        if (!ItemCheck(args.Puller, ent))
            args.Cancel();
    }

    private void OnEquipAttempt(Entity<RoleItemComponent> ent, ref BeingEquippedAttemptEvent args)
    {
        if (!ItemCheck(args.EquipTarget, ent))
            args.Cancel();
    }

    protected bool ItemCheck(EntityUid user, Entity<RoleItemComponent> item)
    {
        if (!_mind.TryGetMind(user, out var mindId, out _))
            return false;

        if (!IsValidRolePrototype(mindId, item.Comp.RoleId))
        {
            if (_net.IsServer)
                _popup.PopupEntity(Loc.GetString(item.Comp.LocalizedPopup), item);

            if (!item.Comp.DamageOnFail.Empty)
                _damageable.TryChangeDamage(user, item.Comp.DamageOnFail, true);

            _audio.PlayPredicted(item.Comp.SoundOnFail, item, user);

            return false;
        }

        return true;
    }

    /// <summary>
    /// Checks if the mind has a role by prototype id.
    /// Check <see cref="AntagPrototype"/>.
    /// </summary>
    /// <returns> True if role found. False if not</returns>/
    public bool IsValidRolePrototype(EntityUid mindId, string roleId)
    {
        if (!_prototype.HasIndex<AntagPrototype>(roleId))
            return false;

        var mindRoles = _role.MindGetAllRoles(mindId);

        foreach (var role in mindRoles)
        {
            if (role.Prototype == roleId)
                return true;
        }

        return false;
    }
}
