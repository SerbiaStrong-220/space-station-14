// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.Pathology.Effects;

public sealed partial class PathologyIgniteEffect : IPathologyEffect
{
    [DataField]
    public float FireStacks = 2f;

    /// <summary>Probability of igniting; 0.001 is one ignition every ~2 minutes.</summary>
    [DataField]
    public float Chance = 0.001f;

    public void ApplyEffect(in PathologyEffectArgs args)
    {
        var ev = new PathologyIgniteEffectEvent(FireStacks, Chance);
        args.EntityManager.EventBus.RaiseLocalEvent(args.Target, ref ev);
    }
}
