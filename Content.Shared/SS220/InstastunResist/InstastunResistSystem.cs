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
            args.cancelled = true;
    }
}

[ByRefEvent]
public record struct StunAttemptEvent(string origin, bool cancelled = false);


