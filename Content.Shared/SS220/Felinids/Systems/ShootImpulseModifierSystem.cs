// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Gravity;
using Content.Shared.SS220.Felinids.Components;
using Content.Shared.Weapons.Ranged.Systems;

namespace Content.Shared.SS220.Felinids.Systems;

public sealed class ShootImpulseModifierSystem : EntitySystem
{
    [Dependency] private readonly SharedGravitySystem _gravity = default!;

    private const float ImpulseOnGroundModifier = 10f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ModifyShootImpulseEvent>(OnModifyShootImpulse);
    }

    private void OnModifyShootImpulse(ModifyShootImpulseEvent args)
    {
        if (!_gravity.IsWeightless(args.Shooter)
            && !HasComp<ShootImpulseComponent>(args.Shooter))
        {
            args.ImpulseModifier = 0f;
            return;
        }

        var impulseModifier = 1f;

        if (TryComp<ShootImpulseComponent>(args.Shooter, out var impulseComp))
        {
            if (_gravity.IsWeightless(args.Shooter))
                impulseModifier *= impulseComp.ImpulseModifier;
            else
            {
                if (impulseComp.RecoilOnGround)
                {
                    impulseModifier *= ImpulseOnGroundModifier;
                    impulseModifier *= impulseComp.ImpulseModifier;
                }
                else
                    args.ImpulseModifier = 0f;
            }
        }

        args.ImpulseModifier = impulseModifier;
    }

}
