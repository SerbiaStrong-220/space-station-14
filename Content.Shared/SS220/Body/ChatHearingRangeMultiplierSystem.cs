using Content.Shared.Body;
using Content.Shared.SS220.Body.Events;
using JetBrains.Annotations;

namespace Content.Shared.SS220.Body;

[UsedImplicitly]
public sealed class ChatHearingRangeMultiplierSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChatHearingRangeMultiplierComponent, BodyRelayedEvent<GetHearingRangeMultiplierEvent>>(OnGetHearingRangeMultiplier);
    }

    private void OnGetHearingRangeMultiplier(Entity<ChatHearingRangeMultiplierComponent> ent, ref BodyRelayedEvent<GetHearingRangeMultiplierEvent> args)
    {
        if (!ent.Comp.Multipliers.TryGetValue(args.Args.ChatType, out var multiplier))
            return;

        var temp = args.Args;
        temp.Multiplier *= multiplier;
        args.Args = temp;
    }
}
