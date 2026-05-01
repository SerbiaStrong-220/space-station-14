using Content.Shared.SS220.AltBlocking;
using Content.Shared.SS220.Weapons.Ranged.Events;
//using Content.Shared.Weapons.Hitscan.Components;
//using Content.Shared.Weapons.Hitscan.Events;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Player;
using Robust.Shared.Random;
using System.Drawing;

namespace Content.Shared.SS220.Weapons.Ranged.Systems;

public sealed class SharedGasWeaponSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        //SubscribeLocalEvent<HitscanBasicDamageComponent, AttemptHitscanRaycastFiredEvent>(OnHitscanHit);
    }


}
