// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Radiation.Components;
using Content.Shared.Radiation.Events;
using Content.Shared.Radiation.Systems;
using Content.Shared.SS220.Pathology;
using Robust.Server.GameObjects;

namespace Content.Server.SS220.Pathology;

public sealed partial class RadiophasiaSystem : EntitySystem
{
    [Dependency] private SharedPointLightSystem _light = default!;
    [Dependency] private SharedRadiationSystem _radiation = default!;
    [Dependency] private DamageableSystem _damageable = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RadiophasiaComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<RadiophasiaComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<RadiophasiaComponent, PathologySeverityChanged>(OnSeverityChanged);
        SubscribeLocalEvent<RadiophasiaComponent, OnIrradiatedEvent>(OnIrradiated);
    }

    private void OnStartup(Entity<RadiophasiaComponent> ent, ref ComponentStartup args)
    {
        // only strip the glow/radiation on cure if the host didn't already have its own
        ent.Comp.AddedLight = !HasComp<PointLightComponent>(ent.Owner);
        ent.Comp.AddedRadiation = !HasComp<RadiationSourceComponent>(ent.Owner);
        EnsureComp<PointLightComponent>(ent.Owner);
        EnsureComp<RadiationSourceComponent>(ent.Owner);
        Apply(ent, 1);
    }

    private void OnShutdown(Entity<RadiophasiaComponent> ent, ref ComponentShutdown args)
    {
        if (Terminating(ent.Owner))
            return;

        if (ent.Comp.AddedRadiation)
            RemComp<RadiationSourceComponent>(ent.Owner);

        if (ent.Comp.AddedLight)
            RemComp<PointLightComponent>(ent.Owner);
    }

    private void OnSeverityChanged(Entity<RadiophasiaComponent> ent, ref PathologySeverityChanged args)
    {
        if (args.PathologyId == ent.Comp.Pathology)
            Apply(ent, args.CurrentSeverity + 1);
    }

    private void Apply(Entity<RadiophasiaComponent> ent, int stage)
    {
        ent.Comp.Stage = stage;

        _light.SetColor(ent.Owner, ent.Comp.LightColor);
        _light.SetRadius(ent.Owner, ent.Comp.LightRadius * stage);
        _light.SetEnergy(ent.Owner, ent.Comp.LightEnergy * stage);

        _radiation.SetIntensity(ent.Owner, ent.Comp.RadiationIntensity * stage);
    }

    private void OnIrradiated(Entity<RadiophasiaComponent> ent, ref OnIrradiatedEvent args)
    {
        if (ent.Comp.HealPerRad.Empty)
            return;

        _damageable.TryChangeDamage(ent.Owner, ent.Comp.HealPerRad * args.TotalRads, interruptsDoAfters: false);
    }
}
