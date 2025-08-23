// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Numerics;
using Content.Server.Actions;
using Content.Server.AlertLevel;
using Content.Server.Buckle.Systems;
using Content.Server.Chat.Systems;
using Content.Server.Ghost;
using Content.Server.Light.EntitySystems;
using Content.Server.Station.Systems;
using Content.Shared.Alert;
using Content.Shared.Damage;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Systems;
using Content.Shared.SS220.DarkReaper;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Content.Shared.Projectiles;
using Content.Server.Projectiles;
using Content.Shared.Light.Components;

namespace Content.Server.SS220.DarkReaper;

public sealed class DarkReaperSystem : SharedDarkReaperSystem
{
    [Dependency] private readonly GhostSystem _ghost = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly PoweredLightSystem _poweredLight = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly AlertLevelSystem _alertLevel = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly BuckleSystem _buckle = default!;
    [Dependency] private readonly ProjectileSystem _projectile = default!;

    private readonly ISawmill _sawmill = Logger.GetSawmill("DarkReaper");

    [ValidatePrototypeId<AlertPrototype>]
    private const string DeadscoreStage1Alert = "DeadscoreStage1";

    [ValidatePrototypeId<AlertPrototype>]
    private const string DeadscoreStage2Alert = "DeadscoreStage2";

    private const int MaxBooEntities = 30;

    public override void Initialize()
    {
        base.Initialize();
    }

    public override void ChangeForm(EntityUid uid, DarkReaperComponent comp, bool isMaterial)
    {
        var isTransitioning = comp.PhysicalForm != isMaterial;
        base.ChangeForm(uid, comp, isMaterial);

        if (!isTransitioning || isMaterial)
            return;

        if (comp.ActivePortal != null)
        {
            QueueDel(comp.ActivePortal);
            comp.ActivePortal = null;
        }

        if (TryComp<EmbeddedContainerComponent>(uid, out var embeddedContainer))
            _projectile.DetachAllEmbedded((uid, embeddedContainer));
    }

    protected override void CreatePortal(EntityUid uid, DarkReaperComponent comp)
    {
        base.CreatePortal(uid, comp);

        // Make lights blink
        BooInRadius(uid, 6);
    }

    protected override void OnAfterConsumed(Entity<DarkReaperComponent> ent, ref AfterConsumed args)
    {
        base.OnAfterConsumed(ent, ref args);

        if (args is not { Cancelled: false, Target: { } target })
            return;

        if (!ent.Comp.PhysicalForm || !target.IsValid() || EntityManager.IsQueuedForDeletion(target) ||
            !_mobState.IsDead(target))
            return;

        if (!_container.TryGetContainer(ent.Owner, DarkReaperComponent.ConsumedContainerId, out var container))
            return;

        if (!_container.CanInsert(target, container))
            return;

        if (_buckle.IsBuckled(args.Target.Value))
            _buckle.TryUnbuckle(args.Target.Value, args.Target.Value, true);

        // spawn gore
        Spawn(ent.Comp.EntityToSpawnAfterConsuming, Transform(target).Coordinates);

        // randomly drop inventory items
        if (_inventory.TryGetContainerSlotEnumerator(target, out var slots))
        {
            while (slots.MoveNext(out var containerSlot))
            {
                if (containerSlot.ContainedEntity is not { } containedEntity)
                    continue;

                if (!_random.Prob(ent.Comp.InventoryDropProbabilityOnConsumed))
                    continue;

                if (!_container.TryRemoveFromContainer(containedEntity))
                    continue;

                // set random rotation
                _transform.SetLocalRotationNoLerp(containedEntity, Angle.FromDegrees(_random.NextDouble(0, 360)));

                // apply random impulse
                var maxAxisImp = ent.Comp.SpawnOnDeathImpulseStrength;
                var impulseVec = new Vector2(_random.NextFloat(-maxAxisImp, maxAxisImp), _random.NextFloat(-maxAxisImp, maxAxisImp));
                _physics.ApplyLinearImpulse(containedEntity, impulseVec);
            }
        }

        _container.Insert(target, container);
        _damageable.TryChangeDamage(ent.Owner, ent.Comp.HealPerConsume, true, origin: args.Args.User);

        ent.Comp.Consumed++;
        var stageBefore = ent.Comp.CurrentStage;
        UpdateStage(ent, ent.Comp);

        // warn a crew if alert stage is reached
        if (ent.Comp.CurrentStage > stageBefore && ent.Comp.CurrentStage == ent.Comp.AlertStage)
        {
            var reaperXform = Transform(ent);
            var stationUid = _station.GetStationInMap(reaperXform.MapID);
            if (stationUid != null)
                _alertLevel.SetLevel(stationUid.Value, ent.Comp.AlertLevelOnAlertStage, true, true, true, false);

            var announcement = Loc.GetString("dark-reaper-component-announcement");
            var sender = Loc.GetString("comms-console-announcement-title-centcom");
            _chat.DispatchStationAnnouncement(stationUid ?? ent, announcement, sender, false, null, Color.Red);//SS220 CluwneComms
        }

        // update consoom counter alert
        UpdateAlert(ent, ent.Comp);
        Dirty(ent);
    }

    private void UpdateAlert(EntityUid uid, DarkReaperComponent comp)
    {
        _alerts.ClearAlert(uid, DeadscoreStage1Alert);
        _alerts.ClearAlert(uid, DeadscoreStage2Alert);

        string alert;
        switch (comp.CurrentStage)
        {
            case 1:
                alert = DeadscoreStage1Alert;
                break;
            case 2:
                alert = DeadscoreStage2Alert;
                break;
            default:
                return;
        }

        if (!comp.ConsumedPerStage.TryGetValue(comp.CurrentStage - 1, out var severity))
            severity = 0;

        severity -= comp.Consumed;

        switch (alert)
        {
            case DeadscoreStage1Alert when severity > 3:
                severity = 3; // 3 is a max value our sprite can display at stage 1
                _sawmill.Error("Had to clamp alert severity. It shouldn't happen. Report it to Artur.");
                break;
            case DeadscoreStage2Alert when severity > 8:
                severity = 8; // 8 is a max value our sprite can display at stage 2
                _sawmill.Error("Had to clamp alert severity. It shouldn't happen. Report it to Artur.");
                break;
        }

        if (severity <= 0)
        {
            _alerts.ClearAlert(uid, DeadscoreStage1Alert);
            _alerts.ClearAlert(uid, DeadscoreStage2Alert);
            return;
        }

        _alerts.ShowAlert(uid, alert, (short) severity);
    }

    protected override void OnCompInit(Entity<DarkReaperComponent> ent, ref ComponentStartup args)
    {
        base.OnCompInit(ent, ref args);

        _container.EnsureContainer<Container>(ent, DarkReaperComponent.ConsumedContainerId);

        if (!ent.Comp.RoflActionEntity.HasValue)
            _actions.AddAction(ent, ref ent.Comp.RoflActionEntity, ent.Comp.RoflAction);

        if (!ent.Comp.StunActionEntity.HasValue)
            _actions.AddAction(ent, ref ent.Comp.StunActionEntity, ent.Comp.StunAction);

        if (!ent.Comp.ConsumeActionEntity.HasValue)
            _actions.AddAction(ent, ref ent.Comp.ConsumeActionEntity, ent.Comp.ConsumeAction);

        if (!ent.Comp.MaterializeActionEntity.HasValue)
            _actions.AddAction(ent, ref ent.Comp.MaterializeActionEntity, ent.Comp.MaterializeAction);

        if (!ent.Comp.BloodMistActionEntity.HasValue)
            _actions.AddAction(ent, ref ent.Comp.BloodMistActionEntity, ent.Comp.BloodMistAction);

        UpdateAlert(ent, ent.Comp);
    }

    protected override void OnCompShutdown(Entity<DarkReaperComponent> ent, ref ComponentShutdown args)
    {
        base.OnCompShutdown(ent, ref args);

        _actions.RemoveAction(ent.Owner, ent.Comp.RoflActionEntity);
        _actions.RemoveAction(ent.Owner, ent.Comp.StunActionEntity);
        _actions.RemoveAction(ent.Owner, ent.Comp.ConsumeActionEntity);
        _actions.RemoveAction(ent.Owner, ent.Comp.MaterializeActionEntity);
        _actions.RemoveAction(ent.Owner, ent.Comp.BloodMistActionEntity);
    }

    protected override void DoStunAbility(EntityUid uid, DarkReaperComponent comp)
    {
        base.DoStunAbility(uid, comp);

        // Destroy lights in radius
        var lightQuery = GetEntityQuery<PoweredLightComponent>();
        var entities = _lookup.GetEntitiesInRange(uid, comp.StunAbilityLightBreakRadius);

        foreach (var entity in entities)
        {
            if (!lightQuery.TryGetComponent(entity, out var lightComp))
                continue;

            _poweredLight.TryDestroyBulb(entity, lightComp);
        }
    }

    private void BooInRadius(EntityUid uid, float radius)
    {
        var entities = _lookup.GetEntitiesInRange(uid, radius);

        var booCounter = 0;
        foreach (var ent in entities)
        {
            var handled = _ghost.DoGhostBooEvent(ent);

            if (handled)
                booCounter++;

            if (booCounter >= MaxBooEntities)
                break;
        }
    }

    protected override void DoRoflAbility(EntityUid uid, DarkReaperComponent comp)
    {
        base.DoRoflAbility(uid, comp);

        // Make lights blink
        BooInRadius(uid, 6);
    }
}
