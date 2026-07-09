// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Nutrition;
using Content.Shared.SS220.Virology;

namespace Content.Server.SS220.Virology;

public sealed partial class VirologySystem
{
    private readonly List<EntityUid> _expiredBuf = new();

    private void InitializeContamination()
    {
        SubscribeLocalEvent<VirusContaminantComponent, IngestedEvent>(OnIngested);
    }

    /// <summary>Leaves a copy of a strain on the item, one per composition, (re)setting that strain's own timer.</summary>
    public void Contaminate(EntityUid target, VirusDescriptor descriptor)
    {
        var comp = EnsureComp<VirusContaminantComponent>(target);
        var identity = GetIdentity(descriptor);
        var expiresAt = _timing.CurTime + comp.Duration;

        // refresh only this strain's own deadline if it's already on the item
        foreach (var existing in comp.Viruses)
        {
            if (GetIdentity(existing.Descriptor) == identity)
            {
                existing.ExpiresAt = expiresAt;
                return;
            }
        }

        comp.Viruses.Add(new VirusContaminant { Descriptor = descriptor.Clone(), ExpiresAt = expiresAt });
    }

    private void OnIngested(Entity<VirusContaminantComponent> ent, ref IngestedEvent args)
    {
        foreach (var contaminant in ent.Comp.Viruses)
            AddVirus(args.Target, contaminant.Descriptor.Clone());
    }

    private void TickContamination()
    {
        var now = _timing.CurTime;
        _expiredBuf.Clear();
        var query = EntityQueryEnumerator<VirusContaminantComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            // each strain fades on its own deadline; drop the ones that have run out
            for (var i = comp.Viruses.Count - 1; i >= 0; i--)
            {
                if (now >= comp.Viruses[i].ExpiresAt)
                    comp.Viruses.RemoveAt(i);
            }

            if (comp.Viruses.Count == 0)
                _expiredBuf.Add(uid);
        }

        foreach (var uid in _expiredBuf)
            RemComp<VirusContaminantComponent>(uid);
    }
}
