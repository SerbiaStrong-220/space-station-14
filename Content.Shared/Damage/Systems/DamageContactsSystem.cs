using Content.Shared.Damage.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;
using Content.Shared.SS220.Buckle; // ss220-flesh-kudzu-damage-fix
using Content.Shared.SS220.Vehicle.Components;
using Content.Shared.Stunnable; // ss220-flesh-kudzu-damage-fix

namespace Content.Shared.Damage.Systems;

public sealed class DamageContactsSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;

    /// <summary> We cache some entities to properly handle collision without re entering entity </summary>
    private Dictionary<EntityUid, HashSet<EntityUid>> _updateQueue = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DamageContactsComponent, StartCollideEvent>(OnEntityEnter);
        SubscribeLocalEvent<DamageContactsComponent, EndCollideEvent>(OnEntityExit);

        //SS220 Add stand still time
        SubscribeLocalEvent<DamagedByContactComponent, MoveEvent>(OnMove);
        SubscribeLocalEvent<DamageContactsComponent, MoveEvent>(OnMove);//SS220 Add check for damager movement
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<DamagedByContactComponent>();

        while (query.MoveNext(out var ent, out var damaged))
        {
            if (_timing.CurTime < damaged.NextSecond ||
                _timing.CurTime < damaged.LastMovement + damaged.StandStillTime) //SS220 Add stand still time
                continue;
            damaged.NextSecond = _timing.CurTime + TimeSpan.FromSeconds(1);

            if (damaged.Damage != null)
                _damageable.TryChangeDamage(ent, damaged.Damage, ignoreResistances: damaged.IgnoreResistances, interruptsDoAfters: false); //SS220 Add IgnoreResistances param
        }
        //SS220 Add check for damager movement begin
        HashSet<EntityUid> sourcesHandled = [];

        foreach (var (source, targets) in _updateQueue)
        {
            if (!TryComp<DamageContactsComponent>(source, out var damageContactsComponent))
                continue;

            if (MovedCheckNeededAndNotPassed(damageContactsComponent))
                continue;

            sourcesHandled.Add(source);
            var entitiesToHandle = _physics.GetContactingEntities(source);
            entitiesToHandle.IntersectWith(targets);

            foreach (var target in entitiesToHandle)
            {
                OnEntityContact((source, damageContactsComponent), target);
            }
        }

        foreach (var handledSource in sourcesHandled)
        {
            _updateQueue.Remove(handledSource);
        }
        //SS220 Add check for damager movement end
    }

    private void OnEntityExit(EntityUid uid, DamageContactsComponent component, ref EndCollideEvent args)
    {
        var otherUid = args.OtherEntity;

        if (!TryComp<PhysicsComponent>(otherUid, out var body))
            return;

        var damageQuery = GetEntityQuery<DamageContactsComponent>();
        foreach (var ent in _physics.GetContactingEntities(otherUid, body))

        {
            if (ent == uid)
                continue;

            if (damageQuery.HasComponent(ent))
                return;
        }

        RemComp<DamagedByContactComponent>(otherUid);

        // ss220-flesh-kudzu-damage-fix-start
        if (TryComp<VehicleComponent>(otherUid, out var comp))
        {
            if (comp.Rider != null)
            {
                var riderId = comp.Rider.Value;
                RemComp<DamagedByContactComponent>(riderId);
            }
        }
        // ss220-flesh-kudzu-damage-fix-end
        //SS220 Add check for damager movement begin
        if (_updateQueue.TryGetValue(uid, out var entities))
        {
            entities.Remove(args.OtherEntity);
        }
        //SS220 Add check for damager movement end
    }

    private void OnEntityEnter(EntityUid uid, DamageContactsComponent component, ref StartCollideEvent args)
    {
        var otherUid = args.OtherEntity;

        // ss220-flesh-kudzu-damage-fix-start
        if (TryComp<VehicleComponent>(otherUid, out var comp))
        {
            if (comp.Rider != null)
            {
                var riderId = comp.Rider.Value;
                var damagedByContactRider = EnsureComp<DamagedByContactComponent>(riderId);
                damagedByContactRider.Damage = component.Damage;
            }
        }
        // ss220-flesh-kudzu-damage-fix-end

        if (HasComp<DamagedByContactComponent>(otherUid))
            return;

        if (_whitelistSystem.IsWhitelistPass(component.IgnoreWhitelist, otherUid) ||
            _whitelistSystem.IsBlacklistFail(component.IgnoreBlacklist, otherUid)) //SS220 Add ignore blacklist
            return;

        //SS220 Add check for damager movement begin
        if (MovedCheckNeededAndNotPassed(component))
        {
            if (_updateQueue.TryGetValue(uid, out var entities))
                entities.Add(args.OtherEntity);
            else
                _updateQueue.Add(uid, new HashSet<EntityUid>() { args.OtherEntity });

            return;
        }
        OnEntityContact((uid, component), otherUid);
    }

    private void OnEntityContact(Entity<DamageContactsComponent> source, EntityUid otherUid)
    {
        var (_, component) = source;

        if (component.StunTime is not null)
            _stun.TryParalyze(otherUid, TimeSpan.FromSeconds(component.StunTime.Value), true, null);
        //SS220 Add check for damager movement end

        var damagedByContact = EnsureComp<DamagedByContactComponent>(otherUid);
        damagedByContact.Damage = component.Damage;

        damagedByContact.IgnoreResistances = component.IgnoreResistances; //SS220 Add IgnoreResistances param
        //SS220 Add stand still time begin
        damagedByContact.StandStillTime = component.StandStillTime;
        Dirty(otherUid, damagedByContact);
        //SS220 Add stand still time end
    }

    //SS220 Add stand still time begin
    private void OnMove(Entity<DamagedByContactComponent> ent, ref MoveEvent args)
    {
        var (uid, component) = ent;
        component.LastMovement = _timing.CurTime;
        Dirty(uid, component);
    }
    //SS220 Add stand still time end

    //SS220 Add check for damager movement begin
    private void OnMove(Entity<DamageContactsComponent> ent, ref MoveEvent args)
    {
        if (ent.Comp.StandStillDelay is null)
            return;

        ent.Comp.LastMovedTime = _timing.CurTime;
    }

    private bool MovedCheckNeededAndNotPassed(DamageContactsComponent component)
    {
        return component.StandStillDelay is not null
                && _timing.CurTime < component.LastMovedTime + component.StandStillDelay;
    }
    //SS220 Add check for damager movement end
}
