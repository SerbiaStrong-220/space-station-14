// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Runtime.InteropServices;
using Content.Shared.SS220.Experience.Skill;
using Robust.Client.Player;
using Robust.Shared.Random;

namespace Content.Client.SS220.Experience;

public abstract class InterfaceShuffler<T> where T : UiAnalyzerShuffleChance, new()
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEventBus _eventBus = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private float _shuffleChance = 0f;

    private float _reshuffleChance = 0f;

    Dictionary<object, float> _cachedShuffleFloat = new();
    Dictionary<object, bool> _cachedProbValue = new();

    public void MakeNewRandomChange(float reshuffleChance)
    {
        if (_playerManager.LocalEntity is not { } entity)
            return;

        var ev = new T();
        _eventBus.RaiseLocalEvent(entity, ref ev);

        _reshuffleChance = reshuffleChance;
        _shuffleChance = ev.ShuffleChance;
    }

    protected bool Shuffle(object key)
    {
        ref var valueRef = ref CollectionsMarshal.GetValueRefOrAddDefault(_cachedProbValue, key, out var exists);

        if (!exists || _random.Prob(_reshuffleChance))
            valueRef = _random.Prob(_shuffleChance);

        return valueRef;
    }

    protected float GetRandomBounded(object key, float value, float minValue, float maxValue)
    {
        ref var valueRef = ref CollectionsMarshal.GetValueRefOrAddDefault(_cachedShuffleFloat, key, out var exists);

        if (!exists || _random.Prob(_reshuffleChance))
            valueRef = _random.NextFloat(minValue, maxValue);

        return !_random.Prob(_shuffleChance) ? value : valueRef;
    }

    protected float GetRandomScaleBounded(object key, float value, float minScale, float maxScale)
    {
        ref var valueRef = ref CollectionsMarshal.GetValueRefOrAddDefault(_cachedShuffleFloat, key, out var exists);

        if (!exists || _random.Prob(_reshuffleChance))
            valueRef = value * _random.NextFloat(minScale, maxScale);

        return !_random.Prob(_shuffleChance) ? value : valueRef;
    }

    protected float GetRandomFromScaleAmplitude(object key, float value, float amplitude)
    {
        return GetRandomScaleBounded(key, value, 1f - amplitude, 1f + amplitude);
    }
}
