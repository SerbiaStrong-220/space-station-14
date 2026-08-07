// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.Virology.Effects;

public sealed partial class VirusIgniteEffect : IVirusEffect
{
    [DataField]
    public float FireStacks = 2f;

    /// <summary>Probability of igniting - 0.001 is every ~2 minutes.</summary>
    [DataField]
    public float Chance = 0.001f;

    public void ApplyEffect(in VirusProgressArgs args)
    {
        var ev = new VirusIgniteEffectEvent(FireStacks, Chance);
        args.EntityManager.EventBus.RaiseLocalEvent(args.Carrier, ref ev);
    }
}
