namespace Content.Shared.SS220.InstastunResist;
public sealed partial class InstastunResistSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<InstastunResistComponent, StunAttemptEvent>(OnActiveBlock);
    }

    public void OnActiveBlock(Entity<InstastunResistComponent> ent, ref StunAttemptEvent args)
    {
        if (ent.Comp.ResistedStunTypes.TryGetValue(args.origin, out var resisted) && resisted)
            args.cancelled = true;
    }
}

[ByRefEvent]
public record struct StunAttemptEvent(string origin, bool cancelled = false);


