// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.Pathology.Effects;

public sealed partial class PathologyTemperatureEffect : IPathologyEffect
{
    /// <summary>Target body temperature in kelvin (318.15 = 45 °C).</summary>
    [DataField]
    public float Temperature = 318.15f;

    public void ApplyEffect(in PathologyEffectArgs args)
    {
        var ev = new PathologyTemperatureEffectEvent(Temperature);
        args.EntityManager.EventBus.RaiseLocalEvent(args.Target, ref ev);
    }
}
