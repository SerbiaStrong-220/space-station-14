// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Hands.EntitySystems;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.MartialArts.Effects;

public sealed partial class StealHeldItemCombatEffect : CombatSequenceEffect
{
    public override void Execute(EntityUid user, EntityUid target, MartialArtistComponent artist)
    {
        var hands = Entity.System<SharedHandsSystem>();

        var held = hands.EnumerateHeld(target);

        if (held.TryFirstOrNull(out var item))
            hands.PickupOrDrop(user, item.Value);
    }
}
