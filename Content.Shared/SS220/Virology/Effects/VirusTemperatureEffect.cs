// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.Virology.Effects;

public sealed partial class VirusTemperatureEffect : IVirusEffect
{
    /// <summary>Target body temperature in kelvin (318 = ~45 °C).</summary>
    [DataField]
    public float Temperature = 318f;

    public void ApplyEffect(in VirusProgressArgs args)
    {
        var ev = new VirusTemperatureEffectEvent(Temperature);
        args.EntityManager.EventBus.RaiseLocalEvent(args.Carrier, ref ev);
    }
}
