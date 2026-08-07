// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.Virology.Effects;

[ByRefEvent]
public record struct VirusAggressionEffectEvent(EntityUid Aggressor);

public sealed partial class VirusAggressionEffect : IVirusEffect
{
    public void ApplyEffect(in VirusProgressArgs args)
    {
        var ev = new VirusAggressionEffectEvent(args.Virus);
        args.EntityManager.EventBus.RaiseLocalEvent(args.Carrier, ref ev);
    }
}
