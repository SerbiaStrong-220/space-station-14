using Content.Server.Hands.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;

namespace Content.Server.Transfer
{
    internal class HandToHandSystem : EntitySystem
    {
        [Dependency] private readonly SharedHandsSystem _sharedHandsSystem = default!;
        [Dependency] private readonly HandsSystem _hands = default!;
        [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
        public async void TryGiveItem(EntityUid user, EntityUid target, EntityUid item)
        {
            var doAfterArgs = new DoAfterArgs(user, 3, new AwaitedDoAfterEvent(), null, target: target)
            {
                AttemptFrequency = AttemptFrequency.EveryTick,
                BreakOnDamage = true,
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                NeedHand = true,
                DuplicateCondition = DuplicateConditions.SameTool
            };
            var result = await _doAfter.WaitDoAfter(doAfterArgs);

            if (result != DoAfterStatus.Finished)
                return;

            _hands.TryDrop(user);
            _hands.PickupOrDrop(target, item);
        }
    }
}
// test
