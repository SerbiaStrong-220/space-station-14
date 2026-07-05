// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT

using Content.Shared.Clothing;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.SS220.Weapons.Melee.Events;
using Content.Shared.Inventory;
using Content.Shared.SS220.Grab;

namespace Content.Shared.SS220.PhysicalParameters;

public sealed class NeuralInterfaceSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<NeuralInterfaceSystem, MeleeAttackerEvent>(OnMeleeAttack);

        base.Initialize();
    }
}
