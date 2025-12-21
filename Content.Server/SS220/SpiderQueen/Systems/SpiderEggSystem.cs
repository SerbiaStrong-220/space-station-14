// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.Mind;
using Content.Server.NPC;
using Content.Server.NPC.Systems;
using Content.Server.SS220.SpiderQueen.Components;
using Content.Shared.Examine;
using Content.Shared.Mind.Components;
using Content.Shared.Speech;
using Content.Shared.Spider;
using Content.Shared.Storage;
using Robust.Shared.Map;
using Robust.Shared.Random;
using System.Numerics;

namespace Content.Server.SS220.SpiderQueen.Systems;

public sealed partial class SpiderEggSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly NPCSystem _npc = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SpiderEggComponent, SpeakAttemptEvent>(OnTrySpeak);
        SubscribeLocalEvent<SpiderEggComponent, ExaminedEvent>(OnExamine);
    }
    private void OnTrySpeak(EntityUid uid, SpiderEggComponent comp, ref SpeakAttemptEvent ev)
    {
        ev.Cancel();
    }
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SpiderEggComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            comp.IncubationTime -= frameTime;
            if (comp.IncubationTime > 0)
                continue;

            SpawnProtos(uid, comp);
            QueueDel(uid);
        }
    }

    private void SpawnProtos(EntityUid uid, SpiderEggComponent component)
    {
        var protos = EntitySpawnCollection.GetSpawns(component.SpawnProtos, _random);
        var coordinates = Transform(uid).Coordinates;

        foreach (var proto in protos)
        {
            var ent = Spawn(proto, coordinates);
            if (TryComp<MindContainerComponent>(uid, out var mind) && mind.HasMind)
                _mind.TransferTo(mind.Mind.Value, ent); // transferto сам по себе если не может зарезолвить mind дает return, на проверки должно быть пофиг, но mind нужен
            if (component.EggOwner is { } owner)
                _npc.SetBlackboard(ent, NPCBlackboard.FollowTarget, new EntityCoordinates(owner, Vector2.Zero));
        }
    }
    private void OnExamine(EntityUid uid, SpiderEggComponent comp, ExaminedEvent ev)
    {
        var roundSec = (int)Math.Round(comp.IncubationTime);
        var has = HasComp<SpiderComponent>(ev.Examiner); // у королевы все равно есть SpiderComponent
        if (ev.Examiner == ev.Examined || has)
        {
            ev.PushMarkup(has ? Loc.GetString("SpiderEgg-IncubationYou", ("sec", roundSec)) : Loc.GetString("SpiderEgg-IncubationMe", ("sec", roundSec)));
        }
    }
}
