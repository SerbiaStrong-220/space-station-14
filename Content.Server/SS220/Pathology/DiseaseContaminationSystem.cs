// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Nutrition;
using Content.Shared.SS220.Pathology;
using Robust.Shared.Timing;

namespace Content.Server.SS220.Pathology;

public sealed partial class DiseaseContaminationSystem : EntitySystem
{
    [Dependency] private SharedPathologySystem _pathology = default!;
    [Dependency] private IGameTiming _timing = default!;

    private static readonly TimeSpan ExpiryInterval = TimeSpan.FromSeconds(1);
    private TimeSpan _nextExpiry;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VirusContaminationComponent, IngestedEvent>(OnIngested);
    }

    // Leaves copy of strain on item or food, keeping one copy per composition and (re)setting the survival timer.
    // Don't do doors or god forbid puddles (for now)
    public void Contaminate(EntityUid target, VirusInstance virus)
    {
        var comp = EnsureComp<VirusContaminationComponent>(target);

        var identity = _pathology.GetIdentity(virus);
        var present = false;
        foreach (var existing in comp.Viruses)
        {
            if (_pathology.GetIdentity(existing) != identity)
                continue;

            present = true;
            break;
        }

        if (!present)
            comp.Viruses.Add(virus.Clone());

        comp.ExpiresAt = _timing.CurTime + comp.Duration;
    }

    private void OnIngested(Entity<VirusContaminationComponent> ent, ref IngestedEvent args)
    {
        foreach (var virus in ent.Comp.Viruses)
            _pathology.AddVirus(args.Target, virus.Clone());
    }

    public override void Update(float frameTime)
    {
        if (_timing.CurTime < _nextExpiry)
            return;

        _nextExpiry = _timing.CurTime + ExpiryInterval;

        var query = EntityQueryEnumerator<VirusContaminationComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_timing.CurTime >= comp.ExpiresAt)
                RemComp<VirusContaminationComponent>(uid);
        }
    }
}
