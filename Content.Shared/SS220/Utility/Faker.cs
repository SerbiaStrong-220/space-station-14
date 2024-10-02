using System.Linq;
using System.Text;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.SS220.Utility;

/// <summary>
/// Helper to quickly create somewhat real-looking data. Slow but handy.
/// </summary>
public static class Faker
{
    private static IRobustRandom Random => IoCManager.Resolve<IRobustRandom>();

    public static List<T> FakeList<T>(int amount, Func<T> construct)
    {
        var r = new List<T>();
        for (var i = 0; i < amount; i++)
        {
            r.Add(construct());
        }
        return r;
    }

    public static List<T> FakeList<T>(int minInclusive, int maxInclusive, Func<T> construct)
    {
        return FakeList(Random.Next(minInclusive, maxInclusive + 1), construct);
    }

    public static T[] FakeArray<T>(int amount, Func<T> construct)
    {
        var r = new T[amount];
        for (var i = 0; i < amount; i++)
        {
            r[i] = construct();
        }
        return r;
    }

    public static T[] FakeArray<T>(int minInclusive, int maxInclusive, Func<T> construct)
    {
        return FakeArray(Random.Next(minInclusive, maxInclusive + 1), construct);
    }

    public static string FakeProtoId<T>() where T : class, IPrototype
    {
        if (IoCManager.Resolve<IPrototypeManager>().TryGetRandom<T>(Random, out var proto))
            return proto.ID;
        return "";
    }

    public static string[] FakeProtoIdArray<T>(int amount) where T : class, IPrototype
    {
        var r = new string[amount];
        for (var i = 0; i < amount; i++)
        {
            r[i] = FakeProtoId<T>();
        }
        return r;
    }

    public static string FakeCharacterName()
    {
        return IoCManager.Resolve<IEntitySystemManager>()
            .GetEntitySystem<NamingSystem>()
            .GetName(FakeProtoId<SpeciesPrototype>());
    }

    public static string FakeUsername()
    {
        var words = new string[]
        {
            "clearance",
            "marble",
            "dream",
            "imagine",
            "drawer",
            "fire",
            "ceiling",
            "offspring",
            "carve",
            "hardware",
            "berry",
            "despise",
            "pioneer",
            "killer",
            "limit",
            "check",
            "army",
            "warm",
            "inspiration",
            "spend",
            "tank",
            "warrant",
            "need",
            "root",
            "start",
            "empire",
            "banish",
            "press",
            "outline",
            "responsibility",
            "satisfaction",
            "lane",
            "wear",
            "ideal",
            "deport",
            "grain",
            "exit",
            "spring",
            "mainstream",
            "hunting",
            "attractive",
            "invisible",
            "likely",
            "memorandum",
            "large",
            "topple",
            "reach",
            "short",
            "raise",
            "clarify",
        };
        var random = Random;
        var symbols = "123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ_.";
        var nick = new StringBuilder();
        var wordsCount = random.Next(1, 3);
        var nickWords = new string[wordsCount];
        for (var i = 0; i < nickWords.Length; i++)
        {
            nickWords[i] = random.Pick(words);
        }
        var strategy = random.Next(3);
        if (strategy == 0)
        {
            nick.AppendJoin(random.Pick(new[] { '.', '_' }), nickWords);
        }
        else if (strategy == 1)
        {
            nick.AppendJoin("", nickWords);
        }
        else if (strategy == 2)
        {
            foreach (var w in nickWords)
            {
                nick.Append(char.ToUpper(w[0]));
                nick.Append(w.Substring(1));
            }
        }
        if (random.Prob(0.5f))
        {
            var randomSymbolsCount = Math.Clamp(random.Next(1, 5), 0, Math.Max(32 - nick.Length, 0));
            for (var i = 0; i < randomSymbolsCount; i++)
            {
                nick.Append(symbols[random.Next(symbols.Length)]);
            }
        }
        return nick.ToString();
    }

    public static T From<T>(IReadOnlyList<T> values)
    {
        return Random.Pick(values);
    }

    public static T From<T>(IEnumerable<T> values)
    {
        return values.ElementAt(Random.Next(values.Count()));
    }
}
