// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Random.Helpers;
using Content.Shared.SS220.Hallucination;
using Robust.Client.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
namespace Content.Client.SS220.Hallucination;

public sealed class HallucinationSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] IPrototypeManager _prototypeManager = default!;
    private bool _isActive = false;
    private Dictionary<int, TimeSpan> _hallucinationTotalTimeSpans = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HallucinationComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<HallucinationComponent, ComponentRemove>(OnComponentRemove);
    }
    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (!_isActive)
            return;
        if (!TryComp<HallucinationComponent>(_playerManager.LocalEntity, out var hallucinationComponent))
        {
            throw new Exception("LocalEntity hadnt got HallucinationComponent but system has isActive true!");
        }
        // here goes codish to solve timed Hallucinations
        if (hallucinationComponent.HallucinationCount == 0)
            return;
        foreach (var (key, nextUpdateTime) in hallucinationComponent.NextUpdateTimes)
        {
            // NaN is used for unlimited Hallucination
            if (!float.IsNaN(hallucinationComponent.TimeParams[key].TotalDuration)
                    && _hallucinationTotalTimeSpans.TryGetValue(key, out _))
            {
                var lifeTime = TimeSpan.FromSeconds(hallucinationComponent.TimeParams[key].TotalDuration);
                _hallucinationTotalTimeSpans.Add(key, _gameTiming.CurTime + lifeTime);
            }
            if (_gameTiming.CurTime > nextUpdateTime)
            {
                MakeHallucination(key, hallucinationComponent);
                var timeBetweenHallucination = TimeSpan.FromSeconds(hallucinationComponent.TimeParams[key].BetweenHallucinations);
                hallucinationComponent.NextUpdateTimes[key] = _gameTiming.CurTime + timeBetweenHallucination;
            }
            if (_hallucinationTotalTimeSpans.TryGetValue(key, out var hallucinationTotalTimeSpan)
                && _gameTiming.CurTime > hallucinationTotalTimeSpan)
                hallucinationComponent.RemoveFromRandomEntities(key);
        }
    }

    private void OnComponentInit(Entity<HallucinationComponent> entity, ref ComponentInit args)
    {
        if (entity.Owner != _playerManager.LocalEntity)
            return;
        _isActive = true;
    }
    private void OnComponentRemove(Entity<HallucinationComponent> entity, ref ComponentRemove args)
    {
        if (entity.Owner != _playerManager.LocalEntity)
            return;
        _isActive = false;
        _hallucinationTotalTimeSpans.Clear();
    }
    private void DestroyLocalEntityComponent(HallucinationComponent hallucinationComponent)
    {
        _isActive = false;
        // lets hope this will delete server comp too
        // make it to server
        RemComp<HallucinationComponent>(_playerManager.LocalEntity!.Value);
        Dirty(_playerManager.LocalEntity!.Value, hallucinationComponent);
    }
    private void MakeHallucination(int key, HallucinationComponent hallucinationComponent)
    {
        var randomWeightedPrototypes = _prototypeManager.Index(hallucinationComponent.RandomEntities(key));
        if (!_prototypeManager.TryIndex<EntityPrototype>(randomWeightedPrototypes.Pick(_random), out var randomProto))
            return; // No way i can log it without killing client...
        var randomCoordinates = Transform(_playerManager.LocalEntity!.Value).Coordinates;
        EntityManager.SpawnAtPosition(randomProto.ID, randomCoordinates);
    }
}
