// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.ItemOffer;

namespace Content.Client.SS220.ItemOffer;

public sealed class ItemOfferSystem : SharedItemOfferSystem
{
    protected override void DoItemOffer(EntityUid user, EntityUid target)
    {
        // DO NOTHING ON CLIENT
    }
}
