using Content.Shared.Trigger;
using Content.Shared.Trigger.Components.Effects;

namespace Content.Shared.SS220.Deafenation;

public sealed class DeafeningOnTriggerSystem : EntitySystem
{
    [Dependency] private readonly DeafenationSystem _deafenation = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DeafeningOnTriggerComponent, TriggerEvent>(OnTrigger);
    }

    private void OnTrigger(Entity<DeafeningOnTriggerComponent> ent, ref TriggerEvent args)
    {
        if (args.Key != null && !ent.Comp.KeysIn.Contains(args.Key))
            return;

        var target = ent.Comp.TargetUser ? args.User : ent.Owner;
        if (target == null)
            return;

        _deafenation.DeafenArea(
            target.Value,
            args.User,
            ent.Comp.Range,
            ent.Comp.KnockdownTime,
            ent.Comp.StunTime,
            ent.Comp.Probability
        );

        args.Handled = true;
    }
}
