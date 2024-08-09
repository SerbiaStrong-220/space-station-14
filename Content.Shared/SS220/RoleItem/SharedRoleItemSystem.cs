// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Administration.Managers;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Mind;
using Content.Shared.Pulling.Events;
using Content.Shared.Roles;

namespace Content.Shared.SS220.RoleItem;

public abstract class SharedRoleItemSystem : EntitySystem
{
    [Dependency] private readonly SharedRoleSystem _role = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
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
            return false;

        return true;
    }

    /// <summary>
    /// Checks if the mind has a role by prototype id.
    /// Check <see cref="AntagPrototype"/>.
    /// </summary>
    /// <returns> True if role found. False if not</returns>
    public bool IsValidRolePrototype(EntityUid mindId, string roleId)
    {
        var mindRoles = _role.MindGetAllRoles(mindId);

        foreach (var role in mindRoles)
        {
            if (role.Prototype == roleId)
                return true;
        }

        return false;
    }
}
