// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Body.Systems;

namespace Content.Shared.SS220.Virology.Effects;

public sealed partial class VirusBleedEffect : IVirusEffect
{
    [DataField]
    public float Amount = 1f;

    public void ApplyEffect(in VirusProgressArgs args)
    {
        args.EntityManager.System<SharedBloodstreamSystem>().TryModifyBleedAmount(args.Carrier, Amount);
    }
}
