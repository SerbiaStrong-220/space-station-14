// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Chat.Managers;
using Content.Shared.SS220.Virology.Behaviors;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.SS220.Virology.Behaviors;

public sealed partial class VirusSchizophreniaSystem : EntitySystem
{
    [Dependency] private IChatManager _chat = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VirusSchizophreniaComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(Entity<VirusSchizophreniaComponent> ent, ref ComponentStartup args)
    {
        ent.Comp.NextMessageTime = _timing.CurTime + _random.Next(ent.Comp.MinInterval, ent.Comp.MaxInterval);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<VirusSchizophreniaComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (now < comp.NextMessageTime)
                continue;

            comp.NextMessageTime = now + _random.Next(comp.MinInterval, comp.MaxInterval);

            if (!_proto.Resolve(comp.Pool, out var pool) || pool.Messages.Count == 0)
                continue;

            var index = PickIndex(pool.Messages.Count, comp.LastIndex);
            comp.LastIndex = index;
            VirusChat.SendSelfMessage(_chat, EntityManager, uid, Loc.GetString(pool.Messages[index]));
        }
    }

    // uniform pick that never repeats the previous index (so the same line can't fire twice in a row)
    private int PickIndex(int count, int last)
    {
        if (count <= 1 || last < 0 || last >= count)
            return _random.Next(count);

        var index = _random.Next(count - 1);
        return index >= last ? index + 1 : index;
    }
}
