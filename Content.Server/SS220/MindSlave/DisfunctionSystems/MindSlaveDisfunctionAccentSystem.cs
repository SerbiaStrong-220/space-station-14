// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Speech;
using Robust.Shared.Random;

namespace Content.Server.SS220.MindSlave.DisfunctionComponents;

public sealed class MindSlaveDisfunctionAccentSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    private readonly List<string> _vowels = ["а", "е", "у", "о", "и", "я"];
    private readonly List<string> _consonants = ["в", "п", "р", "к", "м", "т", "с"];

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MindSlaveDisfunctionAccentComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(Entity<MindSlaveDisfunctionAccentComponent> entity, ref AccentGetEvent args)
    {
        var message = args.Message;
        var vowel = _random.Pick(_vowels);
        var consonant = _random.Pick(_consonants);
        args.Message = TryChangeInString(TryChangeInString(message, vowel, consonant, entity.Comp.Prob),
                                                                    consonant, vowel, entity.Comp.Prob);
    }

    private string TryChangeInString(string value, string key, string keyAddition, float prob)
    {
        var result = value;
        var index = value.IndexOf(key);
        if (index != -1)
        {
            if (_random.Prob(prob))
            {
                result = value.Replace(key, key + keyAddition + key);
            }
        }
        return result;
    }
}
