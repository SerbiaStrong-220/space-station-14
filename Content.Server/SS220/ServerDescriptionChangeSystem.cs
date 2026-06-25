// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Random.Helpers;
using Content.Shared.SS220.CCVars;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.SS220;

/// <summary>
/// Uses available systems for EntitySystem to do non Entity things
/// </summary>
public sealed partial class ServerDescriptionChangeSystem : EntitySystem
{
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private IConfigurationManager _configuration = default!;
    [Dependency] private IRobustRandom _random = default!;

    private TimeSpan _nextChangeTime;
    private TimeSpan _changeInterval;

    private bool _changeDescription;
    private Dictionary<string, float> _cachedDescription = new();
    private string _cachedDefaultDesc = string.Empty;

    public override void Initialize()
    {
        base.Initialize();

        _cachedDefaultDesc = _configuration.GetCVar(CVars.GameDesc);

        _configuration.OnValueChanged(CCVars220.GameDescList, UpdateCachedDescriptions, true);
        _configuration.OnValueChanged(CCVars220.GameDescListChangeInterval, x => _changeInterval = TimeSpan.FromSeconds(x), true);

        _configuration.OnValueChanged(CCVars220.GameDescListEnabled, OnEnableSwitched, true);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_changeDescription || _cachedDescription.Count == 0)
            return;

        if (_gameTiming.CurTime < _nextChangeTime)
            return;

        _nextChangeTime = _gameTiming.CurTime + _changeInterval;
        var currentDescription = _random.Pick(_cachedDescription);

        _configuration.SetCVar(CVars.GameDesc, currentDescription);
    }

    private void OnEnableSwitched(bool enabled)
    {
        _changeDescription = enabled;
        _configuration.SetCVar(CVars.GameDesc, _cachedDefaultDesc);
    }

    private void UpdateCachedDescriptions(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            _cachedDescription.Clear();
            return;
        }

        var splitInput = input.Split(';');
        var parsedAny = false;
        foreach (var dictionaryFullString in splitInput)
        {
            var splitDictionaryString = dictionaryFullString.Split('|', 2);
            if (splitDictionaryString.Length < 2)
                continue;

            if (!float.TryParse(splitDictionaryString[0], out var chance))
                continue;

            var description = splitDictionaryString[1];

            if (!parsedAny)
                _cachedDescription.Clear();

            parsedAny = true;
            _cachedDescription.TryAdd(description, chance);
        }
    }
}
