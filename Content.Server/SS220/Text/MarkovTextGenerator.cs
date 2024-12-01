// Code under © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using System.Text;
using Robust.Shared.Random;

namespace Content.Server.SS220.Text;

public sealed class MarkovTextGenerator : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    private Dictionary<string, List<string>> _transitionMatrix = new();

    private readonly char[] _punctuationChars = { ',', '.', '!', '?', ';', ':' };
    private readonly char[] _endSentenceChars = { '.', '!', '?' };

    public void CleatData()
    {
        _transitionMatrix.Clear();
    }

    public void Initialize(IReadOnlyList<string> locPath, int keySize)
    {
        CleatData();

        foreach (var path in locPath)
        {
            StringBuilder text = new(Loc.GetString(path).ToLower());
            foreach (var punctuationChar in _punctuationChars)
            {
                text.Replace($"{punctuationChar}", $" {punctuationChar}");
            }


            var words = text.ToString().Split().ToArray();

            for (int i = 0; i < words.Length - keySize; i++)
            {
                var key = string.Join(' ', words, i, keySize);
                var value = words[i + keySize];
                AddToTransitionMatrix(key, value);
            }
        }
    }

    /// <summary>
    /// Generates text using Markov chain. For initializing used text from <paramref name="locPath"/>.
    /// </summary>
    /// <param name="locPath">From this text will be generated new one.</param>
    /// <returns>Text generated with using Markov chain.</returns>
    public string GenerateText(int wordCount)
    {
        if (_transitionMatrix.Count == 0)
            Log.Error("Tried to generate text, but transition matrix is clear");

        var tokenKey = _random.Pick(_transitionMatrix.Keys.Where(x => !StartsWithAnyChar(x, _punctuationChars)).ToList());
        var previousToken = "";
        var token = tokenKey.Split().Last();
        var text = new StringBuilder(FirstToUpper(tokenKey.Split().Aggregate(TextJoin)));
        for (var step = 0; step < wordCount; step++)
        {
            previousToken = token;
            if (_transitionMatrix.TryGetValue(tokenKey, out var tokens))
                token = _random.Pick(tokens);
            else
                token = _random.Pick(_random.Pick(_transitionMatrix.Keys).Split());

            tokenKey = tokenKey.Split().Skip(1).Append(token).Aggregate(Join);
            text.Append(TextString(previousToken, token));
        }

        return text.Append("..").ToString();
    }

    public string ReplacePunctuationInEnding(string value)
    {
        return new string(value.ToCharArray().Where(x => !_punctuationChars.Contains(x)).ToArray());
    }

    private void AddToTransitionMatrix(string key, string value)
    {
        if (_transitionMatrix.Keys.Contains(key))
            _transitionMatrix[key].Add(value);
        else
            _transitionMatrix.Add(key, [value]);
    }

    private string TextJoin(string a, string b)
    {
        return a + TextString(a, b);
    }

    private string TextString(string a, string b)
    {
        if (a.Length == 0 || b.Length == 0)
            return b;
        if (_endSentenceChars.Contains(a.Last()))
            return " " + FirstToUpper(b);
        if (_punctuationChars.Contains(b[0]))
            return b;
        return " " + b;
    }

    private bool StartsWithAnyChar(string value, char[] chars)
    {
        foreach (var item in chars)
        {
            if (value.StartsWith(item))
                return true;
        }
        return false;
    }

    private string FirstToUpper(string value)
    {
        return char.ToUpper(value[0]) + value.Remove(0, 1);
    }

    private string Join(string a, string b)
    {
        return a + " " + b;
    }
}
