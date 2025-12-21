// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Actions;
using Content.Shared.Mind;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.DarkReaper;

public sealed class DarkReaperRuneSharedSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    private readonly ISawmill _sawmill = Logger.GetSawmill("dark-reaper-spawn");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DarkReaperRuneComponent, ReaperSpawnEvent>(OnSpawnAction);
        SubscribeLocalEvent<DarkReaperRuneComponent, ComponentStartup>(OnInit);
    }

    private void OnInit(Entity<DarkReaperRuneComponent> ent, ref ComponentStartup args)
    {
        if (_net.IsServer)
            _actions.AddAction(ent, ref ent.Comp.SpawnActionEntity, ent.Comp.SpawnAction);
    }

    private void OnSpawnAction(Entity<DarkReaperRuneComponent> ent, ref ReaperSpawnEvent args)
    {
        if (!_net.IsServer)
            return;

        args.Handled = true;

        if (!_prototype.TryIndex<EntityPrototype>(ent.Comp.DarkReaperPrototypeId, out _))
            return;

        if (!_mindSystem.TryGetMind(ent, out var mindId, out var mind))
            return;

        var coords = Transform(ent).Coordinates;
        if (!coords.IsValid(EntityManager))
        {
            _sawmill.Debug("Failed to spawn Dark Reaper: spawn coordinates are invalid!");
            return;
        }

        var reaper = Spawn(ent.Comp.DarkReaperPrototypeId, Transform(ent).Coordinates);
        _mindSystem.TransferTo(mindId, reaper, mind: mind);
        _audio.PlayPvs(ent.Comp.SpawnSound, reaper);

        QueueDel(ent);
    }
}
