// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.FixedPoint;
using Content.Shared.SS220.Experience.Skill;
using Robust.Client.Player;
using Robust.Shared.Random;

namespace Content.Client.SS220.Experience;

public sealed class RandomShuffle<T> where T : ShuffleChanceGetterEvent, new()
{
    [Dependency] private readonly IDynamicTypeFactory _dynamicTypeFactory = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private float _shuffleChance = 0f;

    private float _reshuffleChance = 0f;

    Dictionary<string, float> _cachedShuffleFloat = new();
    Dictionary<string, bool> _cachedShuffleBool = new();

    Dictionary<string, bool> _cachedProbValue = new();

    public RandomShuffle()
    {
        IoCManager.InjectDependencies(this);
    }

    public void MakeNewRandomChange(float reshuffleChance)
    {
        if (_playerManager.LocalEntity is not { } entity)
            return;

        var ev = _dynamicTypeFactory.CreateInstance<T>();
        _entityManager.EventBus.RaiseLocalEvent(entity, ref ev);

        _reshuffleChance = reshuffleChance;
        _shuffleChance = ev.ShuffleChance;
    }

    private bool Shuffle(string key)
    {
        if (!_cachedProbValue.TryGetValue(key, out var cachedValue) || _random.Prob(_reshuffleChance))
        {
            cachedValue = _random.Prob(_shuffleChance);
            _cachedProbValue[key] = cachedValue;
        }

        return cachedValue;
    }

    public bool GetRandomBool(string key, bool value)
    {
        if (_shuffleChance == 0f)
            return value;

        if (!_cachedShuffleBool.TryGetValue(key, out var cachedValue) || _random.Prob(_reshuffleChance))
        {
            cachedValue = _random.Prob(0.5f);
            _cachedShuffleBool[key] = cachedValue;
        }

        return !Shuffle(key) ? value : cachedValue;
    }

    public float GetRandomBounded(string key, float value, float minValue, float maxValue)
    {
        if (_shuffleChance == 0f)
            return value;

        if (!_cachedShuffleFloat.TryGetValue(key, out var cachedValue) || _random.Prob(_reshuffleChance))
        {
            cachedValue = _random.NextFloat(minValue, maxValue);
            _cachedShuffleFloat[key] = cachedValue;
        }

        return !Shuffle(key) ? value : cachedValue;
    }

    public float GetRandomScaleBounded(string key, float value, float minScale, float maxScale)
    {
        if (_shuffleChance == 0f)
            return value;

        if (!_cachedShuffleFloat.TryGetValue(key, out var cachedValue) || _random.Prob(_reshuffleChance))
        {
            cachedValue = value * _random.NextFloat(minScale, maxScale);
            _cachedShuffleFloat[key] = cachedValue;
        }

        return !Shuffle(key) ? value : cachedValue;
    }

    public float GetRandomFromScaleAmplitude(string key, float value, float amplitude)
    {
        return GetRandomScaleBounded(key, value, 1f - amplitude, 1f + amplitude);
    }

    /// <summary>
    /// This method correctly masks random FixedPoint4 to prevent incorrect results
    /// </summary>
    public FixedPoint4 GetRandomBounded(string key, FixedPoint4 value, float minScale, float maxScale)
    {
        var result = GetRandomBounded(key, value.Float(), minScale, maxScale);

        return FixedPoint4.New(result);
    }

    /// <summary>
    /// This method correctly masks random FixedPoint2 to prevent incorrect results
    /// </summary>
    public FixedPoint2 GetRandomBounded(string key, FixedPoint2 value, float minScale, float maxScale)
    {
        var result = GetRandomBounded(key, value.Float(), minScale, maxScale);

        return FixedPoint2.New(result);
    }

    /// <summary>
    /// This method correctly masks random FixedPoint4 to prevent incorrect results
    /// </summary>
    public FixedPoint4 GetRandomFromScaleAmplitude(string key, FixedPoint4 value, float amplitude)
    {
        var result = GetRandomScaleBounded(key, value.Float(), 1f - amplitude, 1f + amplitude);

        return FixedPoint4.New(result);
    }

    /// <summary>
    /// This method correctly masks random FixedPoint2 to prevent incorrect results
    /// </summary>
    public FixedPoint2 GetRandomFromScaleAmplitude(string key, FixedPoint2 value, float amplitude)
    {
        var result = GetRandomScaleBounded(key, value.Float(), 1f - amplitude, 1f + amplitude);

        return FixedPoint2.New(result);
    }
}
