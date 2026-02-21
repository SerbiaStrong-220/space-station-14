using Content.Shared.FCB.AltBlocking;
using Content.Shared.FCB.Weapons.Ranged.Events;
//using Content.Shared.Weapons.Hitscan.Components;
//using Content.Shared.Weapons.Hitscan.Events;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Player;
using Robust.Shared.Random;
using System.Drawing;

namespace Content.Shared.FCB.Weapons.Ranged.Systems;

public sealed class HitscanBlockSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        //SubscribeLocalEvent<HitscanBasicDamageComponent, AttemptHitscanRaycastFiredEvent>(OnHitscanHit);
    }


}
