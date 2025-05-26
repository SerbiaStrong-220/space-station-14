// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Numerics;
using Content.Server.Audio;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.SS220.ForcefieldGenerator;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Server.SS220.ForcefieldGenerator;

public sealed class ForcefieldGeneratorSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly FixtureSystem _fixture = default!;
    [Dependency] private readonly PhysicsSystem _physics = default!;
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly PointLightSystem _pointLight = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly AmbientSoundSystem _ambientSound = default!;

    private EntityQuery<PhysicsComponent> _physicsQuery;

    public override void Initialize()
    {
        SubscribeLocalEvent<ForcefieldGeneratorSS220Component, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<ForcefieldGeneratorSS220Component, SignalReceivedEvent>(OnSignalReceived);
        SubscribeLocalEvent<ForcefieldGeneratorSS220Component, ChargeChangedEvent>(OnChargeChanged);
        SubscribeLocalEvent<ForcefieldSS220Component, DamageChangedEvent>(OnDamageReceived);

        _physicsQuery = GetEntityQuery<PhysicsComponent>();
    }

    private void OnComponentShutdown(Entity<ForcefieldGeneratorSS220Component> entity, ref ComponentShutdown args)
    {
        ClearShieldEntity(entity.Comp);
    }

    private void OnChargeChanged(Entity<ForcefieldGeneratorSS220Component> entity, ref ChargeChangedEvent args)
    {
        UpdateAppearance(entity);
    }

    private void OnSignalReceived(Entity<ForcefieldGeneratorSS220Component> entity, ref SignalReceivedEvent args)
    {
        if (args.Port == entity.Comp.TogglePort)
            SetActive(entity, !entity.Comp.Active);
    }

    private void OnDamageReceived(Entity<ForcefieldSS220Component> entity, ref DamageChangedEvent args)
    {
        if (args.DamageDelta is not { } damageDelta)
            return;

        if (entity.Comp.Generator is not { } generator)
            return;

        if (!Exists(generator))
            return;

        if (!TryComp<ForcefieldGeneratorSS220Component>(generator, out var genComp))
            return;

        var totalEnergyDraw = damageDelta.GetTotal().Float() * genComp.DamageToEnergyCoefficient;
        if (totalEnergyDraw <= 0)
            return;

        _battery.UseCharge(generator, totalEnergyDraw);
        _audio.PlayPvs(entity.Comp.HitSound, entity);
    }

    private EntityUid? GetFieldEntity(ForcefieldGeneratorSS220Component comp)
    {
        if (comp.FieldEntity is { } existing)
        {
            if (!Deleted(existing) && !EntityManager.IsQueuedForDeletion(existing))
                return existing;
        }

        return null;
    }

    private EntityUid EnsureFieldEntity(Entity<ForcefieldGeneratorSS220Component> entity)
    {
        if (GetFieldEntity(entity) is { } existing)
            return existing;

        var newFieldEntity = Spawn(entity.Comp.ShieldProto, Transform(entity).Coordinates);
        entity.Comp.FieldEntity = newFieldEntity;

        if (TryComp<ForcefieldSS220Component>(newFieldEntity, out var forcefieldComp))
        {
            forcefieldComp.Generator = entity;
            Dirty(entity, forcefieldComp);
        }

        UpdateFieldFixture(entity);

        return newFieldEntity;
    }

    private void ClearShieldEntity(ForcefieldGeneratorSS220Component comp)
    {
        if (comp.FieldEntity != null)
        {
            QueueDel(comp.FieldEntity);
            comp.FieldEntity = null;
        }
    }

    public void SetFieldLength(Entity<ForcefieldGeneratorSS220Component> entity, float length)
    {
        entity.Comp.FieldLength = length;
        Dirty(entity);
        UpdateFieldFixture(entity);
    }

    private void UpdateFieldFixture(Entity<ForcefieldGeneratorSS220Component> entity)
    {
        var shieldEntity = GetFieldEntity(entity.Comp);
        if (!shieldEntity.HasValue)
            return;

        if (!TryComp<FixturesComponent>(shieldEntity, out var fixtures))
            return;

        var oldFixture = _fixture.GetFixtureOrNull(
            shieldEntity.Value,
            ForcefieldGeneratorSS220Component.FIELD_FIXTURE_NAME,
            fixtures);

        if (oldFixture is null)
            return;

        // retain properties that were specified in proto
        var density = oldFixture.Density;
        var mask = oldFixture.CollisionMask;
        var layer = oldFixture.CollisionLayer;

        // define shape
        var shape = new PolygonShape();
        var box = new Box2(
            new Vector2(-entity.Comp.FieldLength / 2, -entity.Comp.FieldThickness / 2),
            new Vector2(entity.Comp.FieldLength / 2, entity.Comp.FieldThickness / 2)
        );
        shape.SetAsBox(box);

        Log.Debug("Shield fixture update");

        _fixture.DestroyFixture(
            shieldEntity.Value,
            ForcefieldGeneratorSS220Component.FIELD_FIXTURE_NAME,
            false,
            manager: fixtures
        );

        _fixture.TryCreateFixture(
            shieldEntity.Value,
            shape,
            ForcefieldGeneratorSS220Component.FIELD_FIXTURE_NAME,
            density: density,
            collisionLayer: layer,
            collisionMask: mask,
            manager: fixtures,
            updates: false
        );

        _fixture.FixtureUpdate(shieldEntity.Value, manager: fixtures);
    }

    private void UpdateFieldPosition(EntityUid entity, ForcefieldGeneratorSS220Component component)
    {
        var fieldEntity = GetFieldEntity(component);
        if (!fieldEntity.HasValue)
            return;

        var genTransform = Transform(entity);
        var pos = Vector2.Transform(new Vector2(0, component.Radius), Matrix3Helpers.CreateRotation(component.Angle));
        _transform.SetCoordinates(fieldEntity.Value, new EntityCoordinates(entity, pos));
        _transform.SetWorldRotation(fieldEntity.Value, _transform.GetWorldRotation(genTransform) + component.Angle);

        // sending entity to nullspace apparently disables canCollide, so we need to ensure its on
        if (_physicsQuery.TryGetComponent(fieldEntity, out var physicsComp))
        {
            if (!physicsComp.CanCollide)
                _physics.SetCanCollide(fieldEntity.Value, true, true, true, body: physicsComp);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ForcefieldGeneratorSS220Component>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.FieldEnabled)
                UpdateFieldPosition(uid, comp);

            if (comp.Active)
            {
                _battery.UseCharge(uid, comp.EnergyUpkeep * frameTime);
                UpdateFieldActivity(new(uid, comp));
            }
        }
    }

    private void SetFieldEnabled(Entity<ForcefieldGeneratorSS220Component> entity, bool enabled)
    {
        if (enabled)
            EnsureFieldEntity(entity);
        else
            ClearShieldEntity(entity.Comp);

        var changed = entity.Comp.FieldEnabled != enabled;
        entity.Comp.FieldEnabled = enabled;

        if (changed)
        {
            Dirty(entity);

            if (!enabled)
            {
                if (TryComp<BatteryComponent>(entity, out var battery) && battery.CurrentCharge <= 0)
                {
                    _popup.PopupEntity(
                        Loc.GetString("forcefield-generator-ss220-unpowered"),
                        entity,
                        Shared.Popups.PopupType.MediumCaution
                    );
                }
                else
                {
                    _popup.PopupEntity(
                        Loc.GetString("forcefield-generator-ss220-disabled"),
                        entity,
                        Shared.Popups.PopupType.Medium
                    );
                }
            }
            else
            {
                _popup.PopupEntity(
                    Loc.GetString("forcefield-generator-ss220-enabled"),
                    entity,
                    Shared.Popups.PopupType.Medium
                );
            }

            var sound = enabled ? entity.Comp.GeneratorOnSound : entity.Comp.GeneratorOffSound;
            _audio.PlayPvs(sound, entity);
            _ambientSound.SetAmbience(entity, enabled);
        }
    }

    private void UpdateFieldActivity(Entity<ForcefieldGeneratorSS220Component> entity)
    {
        if (entity.Comp.Active)
        {
            if (TryComp<BatteryComponent>(entity, out var battery) && battery.CurrentCharge > 0)
            {
                SetFieldEnabled(entity, true);
                return;
            }
        }

        SetFieldEnabled(entity, false);
    }

    private void UpdateAppearance(Entity<ForcefieldGeneratorSS220Component> entity)
    {
        _appearance.SetData(entity, ForcefieldGeneratorVisual.Active, entity.Comp.Active);

        if (TryComp<BatteryComponent>(entity, out var battery))
        {
            var charge = battery.CurrentCharge / battery.MaxCharge;
            var powerVisualState = charge switch
            {
                (>= 0.95f) => ForcefieldGeneratorVisual.Power_4,
                (>= 0.65f and < 0.95f) => ForcefieldGeneratorVisual.Power_3,
                (>= 0.3f and < 0.65f) => ForcefieldGeneratorVisual.Power_2,
                (< 0.3f) => ForcefieldGeneratorVisual.Power_1,
                _ => ForcefieldGeneratorVisual.Power_1
            };

            _appearance.SetData(
                entity,
                ForcefieldGeneratorVisual.Power_1,
                powerVisualState == ForcefieldGeneratorVisual.Power_1);
            _appearance.SetData(
                entity,
                ForcefieldGeneratorVisual.Power_2,
                powerVisualState == ForcefieldGeneratorVisual.Power_2);
            _appearance.SetData(
                entity,
                ForcefieldGeneratorVisual.Power_3,
                powerVisualState == ForcefieldGeneratorVisual.Power_3);
            _appearance.SetData(
                entity,
                ForcefieldGeneratorVisual.Power_4,
                powerVisualState == ForcefieldGeneratorVisual.Power_4);
        }
    }

    public void SetActive(Entity<ForcefieldGeneratorSS220Component> entity, bool active)
    {
        Log.Debug("Field active: " + active.ToString());

        if (active)
        {
            if (!TryComp<BatteryComponent>(entity, out var battery) || battery.CurrentCharge < battery.MaxCharge)
                return;
        }

        entity.Comp.Active = active;
        _pointLight.SetEnabled(entity, active);

        UpdateAppearance(entity);
        UpdateFieldActivity(entity);
        Dirty(entity);
    }
}
