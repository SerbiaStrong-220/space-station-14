// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Linq;
using Content.Shared.Random.Helpers;
using Content.Shared.SS220.Hallucination;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Spawners;
using System.Numerics;
using Robust.Shared.Timing;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;
namespace Content.Client.SS220.Hallucination;

public sealed class HallucinationSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HallucinationComponent, ComponentHandleState>(HandleState);
    }
    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (!TryComp<HallucinationComponent>(_playerManager.LocalEntity, out var hallucinationComponent))
            return;

        for (int i = 0; i < hallucinationComponent.HallucinationSpawnerTimers.Count; i++)
        {
            var nextHallucinationSpawnTime = hallucinationComponent.HallucinationSpawnerTimers[i];
            if (_gameTiming.CurTime > nextHallucinationSpawnTime)
            {
                var hallucination = hallucinationComponent.Hallucinations[i];
                MakeHallucination(hallucination);

                var timeBetweenHallucination = TimeSpan.FromSeconds(hallucination.TimeParams.BetweenHallucinations);
                hallucinationComponent.HallucinationSpawnerTimers[i] = _gameTiming.CurTime + timeBetweenHallucination;
            }
        }
    }
    /// <summary>
    /// Specific handler for hallucination due to bound between timer and hallucinationSettings. Probably saving the same order as in server
    /// </summary>
    private void HandleState(Entity<HallucinationComponent> entity, ref ComponentHandleState args)
    {
        if (args.Current is not HallucinationComponentState state)
            return;

        var hallucinationSymmetricDifference = new List<HallucinationSetting>(entity.Comp.Hallucinations.Union(state.Hallucinations).
                                            Except(entity.Comp.Hallucinations.Intersect(state.Hallucinations)));
        var index = -1;

        foreach (var hallucination in hallucinationSymmetricDifference)
        {
            // handle deleting hallucination in client
            index = entity.Comp.Hallucinations.IndexOf(hallucination);
            if (index != -1)
            {
                entity.Comp.Hallucinations.RemoveAt(index);
                entity.Comp.HallucinationSpawnerTimers.RemoveAt(index);
            }
            //handle adding hallucination to client
            index = state.Hallucinations.IndexOf(hallucination);
            if (index != -1)
            {
                entity.Comp.Hallucinations.Add(hallucination);
                entity.Comp.HallucinationSpawnerTimers.Add(_gameTiming.CurTime);
            }
        }
        // some self-checks in case of my mistake
        DebugTools.Assert(entity.Comp.Hallucinations.Count == state.Hallucinations.Count);
        DebugTools.Assert(entity.Comp.Hallucinations.Count == entity.Comp.HallucinationSpawnerTimers.Count);
    }
    private void MakeHallucination(HallucinationSetting hallucination)
    {
        var randomWeightedPrototypes = _prototypeManager.Index(hallucination.RandomEntities);
        if (!_prototypeManager.TryIndex<EntityPrototype>(randomWeightedPrototypes.Pick(_random), out var randomProto))
            return;

        var spawnedEntityUid = EntityManager.SpawnAtPosition(randomProto.ID,
                                                            Transform(_playerManager.LocalEntity!.Value).Coordinates);
        var randomCoordinates = _transformSystem.GetWorldPosition(_playerManager.LocalEntity!.Value)
                                + new Vector2(_random.NextFloat(-6f, 6f), _random.NextFloat(-6f, 6f));
        _transformSystem.SetWorldPosition(spawnedEntityUid, randomCoordinates);

        var lifeTime = _random.NextFloat(hallucination.TimeParams.HallucinationMinTime, hallucination.TimeParams.HallucinationMaxTime);
        var timedDespawnComp = EnsureComp<TimedDespawnComponent>(spawnedEntityUid);
        timedDespawnComp.Lifetime = lifeTime;
    }
}
