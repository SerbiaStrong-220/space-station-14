// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.DeviceLinking.Systems;
using Content.Server.Electrocution;
using Content.Server.Power.EntitySystems;
using Content.Shared.Buckle.Components;
using Content.Shared.DeviceLinking.Events;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.SS220.ElectricalChair;

public sealed partial class ElectricalChairSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly DeviceLinkSystem _deviceLink = default!;
    [Dependency] private readonly ElectrocutionSystem _electrocution = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ElectricalChairComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ElectricalChairComponent, SignalReceivedEvent>(OnSignalReceived);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ElectricalChairComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (!CanDoElectrocution(uid, component) ||
                _timing.CurTime < component.NextDamageSecond ||
                !TryComp<StrapComponent>(uid, out var strap) ||
                strap.BuckledEntities.Count <= 0)
                continue;

            foreach (var target in strap.BuckledEntities)
            {
                if (_electrocution.TryDoElectrocution(target,
                    uid,
                    component.DamagePerSecond,
                    TimeSpan.FromSeconds(component.ElectrocuteTime),
                    true,
                    _random.NextFloat(0.8f, 1.2f),
                    ignoreInsulation: true) &&
                    component.PlaySoundOnShock)
                {
                    _audio.PlayPvs(component.ShockNoises, target, AudioParams.Default.WithVolume(component.ShockVolume));
                }
            }

            component.NextDamageSecond = _timing.CurTime + TimeSpan.FromSeconds(1);
        }
    }

    private void OnMapInit(Entity<ElectricalChairComponent> ent, ref MapInitEvent args)
    {
        _deviceLink.EnsureSinkPorts(ent, ent.Comp.TogglePort, ent.Comp.OnPort, ent.Comp.OffPort);
    }

    private void OnSignalReceived(Entity<ElectricalChairComponent> ent, ref SignalReceivedEvent args)
    {
        if (args.Port == ent.Comp.TogglePort)
            SetState(ent, !ent.Comp.Enabled);
        else if (args.Port == ent.Comp.OnPort)
            SetState(ent, true);
        else if (args.Port == ent.Comp.OffPort)
            SetState(ent, false);
    }

    private void SetState(EntityUid uid, bool value, ElectricalChairComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.Enabled = value;
    }

    private bool CanDoElectrocution(EntityUid uid, ElectricalChairComponent component)
    {
        var xform = Transform(uid);
        if (!xform.Anchored || !this.IsPowered(uid, EntityManager))
            return false;

        return component.Enabled;
    }
}
