// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Body.Systems;

namespace Content.Shared.SS220.Pathology.Effects;

public sealed partial class PathologyBleedEffect : IPathologyEffect
{
    [DataField]
    public float Amount = 1f;

    public void ApplyEffect(in PathologyEffectArgs args)
    {
        args.EntityManager.System<SharedBloodstreamSystem>().TryModifyBleedAmount(args.Target, Amount);
    }
}
