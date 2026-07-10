// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage.Systems;
using Content.Shared.Radiation.Components;
using Content.Shared.Radiation.Events;
using Content.Shared.Radiation.Systems;
using Content.Shared.SS220.Virology.Behaviors;

namespace Content.Server.SS220.Virology.Behaviors;

public sealed partial class VirusRadiophasiaSystem : EntitySystem
{
    [Dependency] private SharedRadiationSystem _radiation = default!;
    [Dependency] private DamageableSystem _damageable = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VirusRadiophasiaComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<VirusRadiophasiaComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<VirusRadiophasiaComponent, OnIrradiatedEvent>(OnIrradiated);
    }

    private void OnStartup(Entity<VirusRadiophasiaComponent> ent, ref ComponentStartup args)
    {
        if (TryComp<RadiationSourceComponent>(ent.Owner, out var existing))
            ent.Comp.PreviousIntensity = existing.Intensity;
        else
            ent.Comp.AddedRadiation = true;

        EnsureComp<RadiationSourceComponent>(ent.Owner);
        _radiation.SetIntensity(ent.Owner, ent.Comp.RadiationIntensity);
    }

    private void OnShutdown(Entity<VirusRadiophasiaComponent> ent, ref ComponentShutdown args)
    {
        if (Terminating(ent.Owner))
            return;

        if (ent.Comp.AddedRadiation)
            RemComp<RadiationSourceComponent>(ent.Owner);
        else if (ent.Comp.PreviousIntensity is { } previous)
            _radiation.SetIntensity(ent.Owner, previous);
    }

    private void OnIrradiated(Entity<VirusRadiophasiaComponent> ent, ref OnIrradiatedEvent args)
    {
        if (ent.Comp.HealPerRad.Empty)
            return;

        // host is radiation emitter so this will prevent self-heal
        var externalRads = args.TotalRads - ent.Comp.RadiationIntensity * args.FrameTime;
        if (externalRads <= 0f)
            return;

        _damageable.TryChangeDamage(ent.Owner, ent.Comp.HealPerRad * externalRads, interruptsDoAfters: false);
    }
}
