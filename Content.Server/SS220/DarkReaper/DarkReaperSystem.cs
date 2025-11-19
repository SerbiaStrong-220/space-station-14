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
using Robust.Shared.Prototypes;

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

    private readonly ProtoId<AlertPrototype> _deadscoreStage1Alert = "DeadscoreStage1";

    private readonly ProtoId<AlertPrototype> _deadscoreStage2Alert = "DeadscoreStage2";

    private const int MaxBooEntities = 30;

    public override void Initialize()
    {
        base.Initialize();
    }

    public override void ChangeForm(Entity<DarkReaperComponent> entity, bool isMaterial)
    {
        var isTransitioning = entity.Comp.PhysicalForm != isMaterial;
        base.ChangeForm(entity, isMaterial);

        if (!isTransitioning || isMaterial)
            return;

        if (entity.Comp.ActivePortal != null)
        {
            QueueDel(entity.Comp.ActivePortal);
            entity.Comp.ActivePortal = null;
        }

        if (TryComp<EmbeddedContainerComponent>(entity, out var embeddedContainer))
            _projectile.DetachAllEmbedded((entity, embeddedContainer));
    }

    protected override void CreatePortal(Entity<DarkReaperComponent> entity)
    {
        base.CreatePortal(entity);

        // Make lights blink
        BooInRadius(entity, 6);
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
        UpdateAlert(ent);
        Dirty(ent);
    }

    private void UpdateAlert(Entity<DarkReaperComponent> entity)
    {
        _alerts.ClearAlert(entity.Owner, _deadscoreStage1Alert);
        _alerts.ClearAlert(entity.Owner, _deadscoreStage2Alert);

        string alert;
        switch (entity.Comp.CurrentStage)
        {
            case 1:
                alert = _deadscoreStage1Alert;
                break;
            case 2:
                alert = _deadscoreStage2Alert;
                break;
            default:
                return;
        }

        if (!entity.Comp.ConsumedPerStage.TryGetValue(entity.Comp.CurrentStage - 1, out var severity))
            severity = 0;

        severity -= entity.Comp.Consumed;

        if (alert == _deadscoreStage1Alert && severity > 3)
        {
            severity = 3; // 3 is a max value our sprite can display at stage 1
            Log.Error("Had to clamp alert severity. It shouldn't happen. Report it.");
        }

        if (alert == _deadscoreStage2Alert && severity > 8)
        {
            severity = 8; // 8 is a max value our sprite can display at stage 2
            Log.Error("Had to clamp alert severity. It shouldn't happen. Report it.");
        }

        if (severity <= 0)
        {
            _alerts.ClearAlert(entity.Owner, _deadscoreStage1Alert);
            _alerts.ClearAlert(entity.Owner, _deadscoreStage2Alert);
            return;
        }

        _alerts.ShowAlert(entity.Owner, alert, (short)severity);
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

        UpdateAlert(ent);
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

    protected override void DoStunAbility(Entity<DarkReaperComponent> entity)
    {
        base.DoStunAbility(entity);

        // Destroy lights in radius
        var poweredLightEntities = _lookup.GetEntitiesInRange<PoweredLightComponent>(Transform(entity).Coordinates, entity.Comp.StunAbilityLightBreakRadius);

        foreach (var lightEntity in poweredLightEntities)
        {
            _poweredLight.TryDestroyBulb(lightEntity);
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
