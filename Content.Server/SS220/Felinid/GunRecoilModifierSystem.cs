using Content.Shared.SS220.Felinid;
using Content.Shared.SS220.Felinid.Components;
using Content.Shared.Stunnable;
using Robust.Shared.Random;

namespace Content.Server.SS220.Felinid;

public sealed partial class GunRecoilModifierSystem : SharedGunRecoilModifierSystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedStunSystem _stun = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GunRecoilModifierComponent, GunRecoilImpulseEvent>(OnGunRecoilImpulse);
    }

    private void OnGunRecoilImpulse(Entity<GunRecoilModifierComponent> ent, ref GunRecoilImpulseEvent args)
    {
        if (_random.Prob(args.KnockdownChance))
            _stun.TryKnockdown(ent.Owner, args.KnockdownTime, drop: true);
    }
}
