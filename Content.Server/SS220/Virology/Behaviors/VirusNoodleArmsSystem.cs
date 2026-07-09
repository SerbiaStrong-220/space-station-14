// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Hands;
using Content.Shared.SS220.Virology.Behaviors;
using Content.Shared.Standing;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.SS220.Virology.Behaviors;

public sealed partial class VirusNoodleArmsSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IGameTiming _timing = default!;

    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);
    private TimeSpan _nextUpdate;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VirusNoodleArmsComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<VirusNoodleArmsComponent, DidEquipHandEvent>(OnDidEquip);
    }

    private void OnStartup(Entity<VirusNoodleArmsComponent> ent, ref ComponentStartup args)
    {
        if (ent.Comp.AlwaysDrop)
            DropAll(ent.Owner);
    }

    private void OnDidEquip(Entity<VirusNoodleArmsComponent> ent, ref DidEquipHandEvent args)
    {
        if (ent.Comp.AlwaysDrop)
            DropAll(ent.Owner);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextUpdate)
            return;

        _nextUpdate = _timing.CurTime + UpdateInterval;

        var query = EntityQueryEnumerator<VirusNoodleArmsComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.AlwaysDrop)
                continue;

            comp.NextSpasm ??= _timing.CurTime + _random.Next(comp.MinInterval, comp.MaxInterval);
            if (_timing.CurTime >= comp.NextSpasm)
            {
                DropAll(uid);
                comp.NextSpasm = _timing.CurTime + _random.Next(comp.MinInterval, comp.MaxInterval);
            }
        }
    }

    private void DropAll(EntityUid uid)
    {
        var ev = new DropHandItemsEvent();
        RaiseLocalEvent(uid, ref ev);
    }
}
