// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.Audio;
using Content.Server.Popups;
using Content.Server.Power.EntitySystems;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Power;
using Content.Shared.Power.Components;
using Content.Shared.SS220.Forcefield.Components;
using Robust.Server.Audio;
using Robust.Server.GameObjects;

namespace Content.Server.SS220.Forcefield.Systems;

public sealed partial class ForcefieldGeneratorSystem : EntitySystem
{
    [Dependency] private readonly PointLightSystem _pointLight = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly ForcefieldSystem _forcefield = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly AmbientSoundSystem _ambientSound = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly BatterySystem _battery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ForcefieldGeneratorComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ForcefieldGeneratorComponent, SignalReceivedEvent>(OnSignalReceived);
        SubscribeLocalEvent<ForcefieldGeneratorComponent, ChargeChangedEvent>(OnChargeChanged);
        SubscribeLocalEvent<ForcefieldGeneratorComponent, ForcefieldDamageChangedEvent>(OnForcefieldDamageChanged);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ForcefieldGeneratorComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Active)
            {
                _battery.UseCharge(uid, comp.EnergyUpkeep * frameTime);
                UpdateForcefieldActivity((uid, comp));
            }
        }
    }

    private void OnShutdown(Entity<ForcefieldGeneratorComponent> entity, ref ComponentShutdown args)
    {
        DeleteForcefieldEntity(entity);
    }

    private void OnChargeChanged(Entity<ForcefieldGeneratorComponent> entity, ref ChargeChangedEvent args)
    {
        UpdateAppearance(entity);
    }

    private void OnSignalReceived(Entity<ForcefieldGeneratorComponent> entity, ref SignalReceivedEvent args)
    {
        if (args.Port == entity.Comp.TogglePort)
            SetActive(entity, !entity.Comp.Active);
    }

    private void OnForcefieldDamageChanged(Entity<ForcefieldGeneratorComponent> entity, ref ForcefieldDamageChangedEvent args)
    {
        if (args.Event.DamageDelta is not { } damageDelta)
            return;

        var totalEnergyDraw = damageDelta.GetTotal().Float() * entity.Comp.DamageToEnergyCoefficient;
        if (totalEnergyDraw <= 0)
            return;

        _battery.UseCharge(entity, totalEnergyDraw);
        _audio.PlayPvs(args.Forcefield.Comp.HitSound, entity);
    }

    public void SetActive(Entity<ForcefieldGeneratorComponent> entity, bool active)
    {
        if (active && (!TryComp<BatteryComponent>(entity, out var battery) || battery.CurrentCharge < battery.MaxCharge))
            return;

        entity.Comp.Active = active;
        _pointLight.SetEnabled(entity, active);

        UpdateAppearance(entity);
        UpdateForcefieldActivity(entity);
        Dirty(entity);
    }

    private void DeleteForcefieldEntity(Entity<ForcefieldGeneratorComponent> entity)
    {
        if (entity.Comp.FieldEntity is not { } forcefield)
            return;

        QueueDel(GetEntity(forcefield));
        entity.Comp.FieldEntity = null;
    }

    private void UpdateAppearance(Entity<ForcefieldGeneratorComponent> entity)
    {
        _appearance.SetData(entity, ForcefieldGeneratorVisual.Active, entity.Comp.Active);

        if (TryComp<BatteryComponent>(entity, out var battery))
        {
            var charge = battery.CurrentCharge / battery.MaxCharge;
            _appearance.SetData(entity, ForcefieldGeneratorVisual.Charge, charge);
        }
    }

    private void SetForcefieldEnabled(Entity<ForcefieldGeneratorComponent> entity, bool enabled)
    {
        if (entity.Comp.FieldEnabled == enabled)
            return;

        if (enabled)
            EnsureForcefieldEntity(entity);
        else
            DeleteForcefieldEntity(entity);

        entity.Comp.FieldEnabled = enabled;
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

    private Entity<ForcefieldComponent> EnsureForcefieldEntity(Entity<ForcefieldGeneratorComponent> entity)
    {
        if (GetForcefieldEntity(entity) is { } existing)
            return existing;

        var forcefieldUid = Spawn(entity.Comp.ShieldProto, Transform(entity).Coordinates);
        entity.Comp.FieldEntity = GetNetEntity(forcefieldUid);
        Dirty(entity);

        var forcefieldComp = EnsureComp<ForcefieldComponent>(forcefieldUid);

        forcefieldComp.Params = entity.Comp.ForcefieldParams;
        forcefieldComp.FieldOwner = GetNetEntity(entity);

        Dirty(forcefieldUid, forcefieldComp);

        _transform.SetParent(forcefieldUid, entity);
        _forcefield.RefreshFigure((forcefieldUid, forcefieldComp));
        return (forcefieldUid, forcefieldComp);
    }

    private Entity<ForcefieldComponent>? GetForcefieldEntity(ForcefieldGeneratorComponent comp)
    {
        var uid = GetEntity(comp.FieldEntity);
        if (uid is null)
            return null;

        if (Deleted(uid) || EntityManager.IsQueuedForDeletion(uid.Value))
            return null;

        if (!TryComp<ForcefieldComponent>(uid, out var forcefieldComp))
            return null;

        return (uid.Value, forcefieldComp);
    }

    private void UpdateForcefieldActivity(Entity<ForcefieldGeneratorComponent> entity)
    {
        if (entity.Comp.Active)
        {
            if (TryComp<BatteryComponent>(entity, out var battery) && battery.CurrentCharge > 0)
            {
                SetForcefieldEnabled(entity, true);
                return;
            }
        }

        SetForcefieldEnabled(entity, false);
    }
}
