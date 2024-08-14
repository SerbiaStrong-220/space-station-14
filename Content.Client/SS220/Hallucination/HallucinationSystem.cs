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
namespace Content.Client.SS220.Hallucination;

public sealed class HallucinationSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    private Dictionary<int, TimeSpan> _hallucinationTotalTimeSpans = new();
    private Dictionary<int, TimeSpan> _nextUpdateTimes = new();
    // private Random _random = new Random();
    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (!TryComp<HallucinationComponent>(_playerManager.LocalEntity, out var hallucinationComponent))
            return;

        foreach (var key in _nextUpdateTimes.Keys.Except(hallucinationComponent.TimeParams.Keys))
        {
            _nextUpdateTimes.Remove(key);
            _hallucinationTotalTimeSpans.Remove(key);
        }

        foreach (var (key, timeParams) in hallucinationComponent.TimeParams)
        {

            if (_nextUpdateTimes.TryAdd(key, _gameTiming.CurTime + TimeSpan.FromSeconds(timeParams.BetweenHallucinations))
                && !float.IsNaN(hallucinationComponent.TimeParams[key].TotalDuration))
                _hallucinationTotalTimeSpans.TryAdd(key, _gameTiming.CurTime + TimeSpan.FromSeconds(timeParams.TotalDuration));
            // NaN is used for unlimited Hallucination
            if (_gameTiming.CurTime > _nextUpdateTimes[key])
            {
                MakeHallucination(key, hallucinationComponent);
                var timeBetweenHallucination = TimeSpan.FromSeconds(hallucinationComponent.TimeParams[key].BetweenHallucinations);
                _nextUpdateTimes[key] = _gameTiming.CurTime + timeBetweenHallucination;
            }
            if (_hallucinationTotalTimeSpans.TryGetValue(key, out var hallucinationTotalTimeSpan)
                && _gameTiming.CurTime > hallucinationTotalTimeSpan)
            {
                hallucinationComponent.RemoveFromRandomEntities(key);
                Dirty(_playerManager.LocalEntity.Value, hallucinationComponent);
                _hallucinationTotalTimeSpans.Remove(key);
            }
        }
    }
    private void MakeHallucination(int key, HallucinationComponent hallucinationComponent)
    {
        var randomWeightedPrototypes = _prototypeManager.Index(hallucinationComponent.RandomEntities(key));
        if (!_prototypeManager.TryIndex<EntityPrototype>(randomWeightedPrototypes.Pick(_random), out var randomProto))
            return; // No way i can log it without killing client...

        var spawnedEntityUid = EntityManager.SpawnAtPosition(randomProto.ID, Transform(_playerManager.LocalEntity!.Value).Coordinates);

        var randomCoordinates = _transformSystem.GetWorldPosition(_playerManager.LocalEntity!.Value)
                                             + new Vector2(_random.NextFloat(-6f, 6f), _random.NextFloat(-6f, 6f));
        _transformSystem.SetWorldPosition(spawnedEntityUid, randomCoordinates);
        var lifeTime = _random.NextFloat(hallucinationComponent.TimeParams[key].HallucinationMinTime,
                                    hallucinationComponent.TimeParams[key].HallucinationMaxTime);

        var timedDespawnComp = EnsureComp<TimedDespawnComponent>(spawnedEntityUid);
        timedDespawnComp.Lifetime = lifeTime;
    }
}
