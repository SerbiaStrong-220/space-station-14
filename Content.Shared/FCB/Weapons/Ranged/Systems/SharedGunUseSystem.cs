// © FCB, MIT, full text: https://github.com/Free-code-base-14/space-station-14/blob/master/LICENSE.TXT
using Content.Shared.FCB.Weapons.Ranged.Events;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Input;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;

namespace Content.Shared.FCB.Weapons.Ranged.Systems;
public sealed class SharedGunUseSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;

}
