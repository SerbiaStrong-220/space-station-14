using Content.Shared.Popups;
using Content.Shared.SS220.AltBlocking;
//using Content.Shared.Weapons.Hitscan.Components;
//using Content.Shared.Weapons.Hitscan.Events;
using Content.Shared.SS220.Weapons.Components;
using Content.Shared.SS220.Weapons.Ranged.Events;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Random;
using System.Drawing;

namespace Content.Shared.SS220.Weapons.Ranged.Systems;

public abstract class SharedGasWeaponSystem : EntitySystem
{

    [Dependency] protected readonly SharedContainerSystem _container = default!;
    [Dependency] protected readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        //SubscribeLocalEvent<HitscanBasicDamageComponent, AttemptHitscanRaycastFiredEvent>(OnHitscanHit);
    }


}
