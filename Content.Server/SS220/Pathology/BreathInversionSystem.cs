// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Metabolism;
using Content.Shared.SS220.Pathology;

namespace Content.Server.SS220.Pathology;

public sealed partial class BreathInversionSystem : EntitySystem
{
    [Dependency] private BodySystem _body = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BreathInversionComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<BreathInversionComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<LungComponent, BodyRelayedEvent<ApplyBreathInversionEvent>>(OnApply);
        SubscribeLocalEvent<LungComponent, BodyRelayedEvent<RestoreBreathInversionEvent>>(OnRestore);
    }

    private void OnStartup(Entity<BreathInversionComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<BodyComponent>(ent, out var body))
            return;

        var ev = new ApplyBreathInversionEvent();
        _body.RelayEvent((ent.Owner, body), ref ev);
    }

    private void OnShutdown(Entity<BreathInversionComponent> ent, ref ComponentShutdown args)
    {
        if (Terminating(ent.Owner))
            return;

        if (!TryComp<BodyComponent>(ent, out var body))
            return;

        var ev = new RestoreBreathInversionEvent();
        _body.RelayEvent((ent.Owner, body), ref ev);
    }

    private void OnApply(Entity<LungComponent> lung, ref BodyRelayedEvent<ApplyBreathInversionEvent> args)
    {
        if (!TryComp<BreathInversionComponent>(args.Body, out var inv)
            || !TryComp<MetabolizerComponent>(lung, out var metab)
            || metab.MetabolizerTypes is not { } types)
            return;

        if (!inv.Original.TryAdd(lung.Owner, [.. types]))
            return;

        var invertToOxygen = types.Overlaps(inv.NitrogenBreathers);
        types.Clear();
        types.Add(invertToOxygen ? inv.OxygenBreatherType : inv.NitrogenBreatherType);
        Dirty(lung.Owner, metab);
    }

    private void OnRestore(Entity<LungComponent> lung, ref BodyRelayedEvent<RestoreBreathInversionEvent> args)
    {
        if (!TryComp<BreathInversionComponent>(args.Body, out var inv)
            || !inv.Original.TryGetValue(lung.Owner, out var original)
            || !TryComp<MetabolizerComponent>(lung, out var metab)
            || metab.MetabolizerTypes is not { } types)
            return;

        // put the lung's original metabolizer types back when cured
        types.Clear();
        types.UnionWith(original);
        inv.Original.Remove(lung.Owner); // drop the snapshot so a later re-apply snapshots afresh
        Dirty(lung.Owner, metab);
    }
}

[ByRefEvent]
public readonly record struct ApplyBreathInversionEvent;

[ByRefEvent]
public readonly record struct RestoreBreathInversionEvent;
