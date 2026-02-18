// © FCB, MIT, full text: https://github.com/Free-code-base-14/space-station-14/blob/master/LICENSE.TXT
using Robust.Shared.Utility;

namespace Content.Shared.FCB.InstastunResist;
public sealed partial class InstastunResistSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<InstastunResistComponent, StunAttemptEvent>(OnStunAttempt);
    }

    public void OnStunAttempt(Entity<InstastunResistComponent> ent, ref StunAttemptEvent args)
    {
        if (ent.Comp.ResistedStunTypes.Contains(args.Origin))
            args.stunCancelled = true;
    }
}

[ByRefEvent]
public record struct StunAttemptEvent(StunSource Origin, bool stunCancelled = false);

public enum StunSource : byte
{
    Creampie = 0,
    Projectile = 1 //Works for StunOnCollide projectiles
}


