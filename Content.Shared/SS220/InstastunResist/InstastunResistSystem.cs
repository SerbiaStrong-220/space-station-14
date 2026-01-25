// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
namespace Content.Shared.SS220.InstastunResist;
public sealed partial class InstastunResistSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<InstastunResistComponent, StunAttemptEvent>(OnStunAttempt);
    }

    public void OnStunAttempt(Entity<InstastunResistComponent> ent, ref StunAttemptEvent args)
    {
        if (ent.Comp.ResistedStunTypes.TryGetValue(args.origin, out var resisted) && resisted)
            args.stunCancelled = true;
    }
}

[ByRefEvent]
public record struct StunAttemptEvent(StunSource origin, bool stunCancelled = false);

public enum StunSource : byte
{
    Creampie = 0,
    Projectile = 1 //Works for StunOnCollide projectiles
}


