// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Virology;
using Content.Shared.SS220.Virology.Effects;

namespace Content.Server.SS220.Virology;

public sealed partial class VirusAggressionSystem : EntitySystem
{
    [Dependency] private VirologySystem _virology = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VirusHolderComponent, VirusAggressionEffectEvent>(OnAggression);
    }

    private void OnAggression(Entity<VirusHolderComponent> ent, ref VirusAggressionEffectEvent args)
    {
        foreach (var strain in _virology.GetStrains(ent))
        {
            if (strain.Owner == args.Aggressor)
                continue;

            _virology.RemoveVirus(strain);
        }
    }
}
