// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Atmos.Components;
using Content.Shared.Gravity;
using Content.Shared.SS220.Felinids.Components;
using Content.Shared.Weapons.Ranged.Systems;

namespace Content.Shared.SS220.Felinids.Systems;

public sealed class ShootImpulseModifierSystem : EntitySystem
{
    [Dependency] private readonly SharedGravitySystem _gravity = default!;

    private float _impulseOnGroundModifier = 5f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ModifyShootImpulseEvent>(OnModifyShootImpulse);
    }

    private void OnModifyShootImpulse(ModifyShootImpulseEvent args)
    {
        if (!CanTakeImpulse(args.Shooter))
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
                    impulseModifier *= _impulseOnGroundModifier;
                    impulseModifier *= impulseComp.ImpulseModifier;
                }
                else
                    args.ImpulseModifier = 0f;
            }
        }

        args.ImpulseModifier = impulseModifier;
    }

    private bool CanTakeImpulse(EntityUid uid)
    {
        if (TryComp<MovedByPressureComponent>(uid, out var pressureComp)
        && !pressureComp.Enabled)
            return false;

        if (!_gravity.IsWeightless(uid)
        && !HasComp<ShootImpulseComponent>(uid))
            return false;

        return true;
    }

}
