// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Nutrition;
using Content.Shared.SS220.Virology;
using Robust.Shared.Timing;

namespace Content.Server.SS220.Virology;

public sealed partial class VirusContaminationSystem : EntitySystem
{
    [Dependency] private VirologySystem _virology = default!;
    [Dependency] private IGameTiming _timing = default!;

    private static readonly TimeSpan ExpiryInterval = TimeSpan.FromSeconds(1);
    private TimeSpan _nextExpiry;
    private readonly List<EntityUid> _expiredBuf = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VirusContaminantComponent, IngestedEvent>(OnIngested);
    }

    /// <summary>Leaves a copy strain on item, one per composition, (re)setting the survival timer.</summary>
    public void Contaminate(EntityUid target, VirusDescriptor descriptor)
    {
        var comp = EnsureComp<VirusContaminantComponent>(target);
        var identity = _virology.GetIdentity(descriptor);

        foreach (var existing in comp.Viruses)
        {
            if (_virology.GetIdentity(existing) == identity)
            {
                comp.ExpiresAt = _timing.CurTime + comp.Duration;
                return;
            }
        }

        comp.Viruses.Add(descriptor.Clone());
        comp.ExpiresAt = _timing.CurTime + comp.Duration;
    }

    private void OnIngested(Entity<VirusContaminantComponent> ent, ref IngestedEvent args)
    {
        foreach (var descriptor in ent.Comp.Viruses)
            _virology.AddVirus(args.Target, descriptor.Clone());
    }

    public override void Update(float frameTime)
    {
        if (_timing.CurTime < _nextExpiry)
            return;

        _nextExpiry = _timing.CurTime + ExpiryInterval;

        _expiredBuf.Clear();
        var query = EntityQueryEnumerator<VirusContaminantComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_timing.CurTime >= comp.ExpiresAt)
                _expiredBuf.Add(uid);
        }

        foreach (var uid in _expiredBuf)
            RemComp<VirusContaminantComponent>(uid);
    }
}
