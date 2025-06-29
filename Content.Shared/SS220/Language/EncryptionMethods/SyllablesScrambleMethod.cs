// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Content.Shared.Random.Helpers;
using Content.Shared.SS220.Language.Systems;
using Robust.Shared.Random;

namespace Content.Shared.SS220.Language.EncryptionMethods;

/// <summary>
/// Scramble a message depending on its length using a specific list of syllables
/// </summary>
public sealed partial class SyllablesScrambleMethod : ScrambleMethod
{
    /// <summary>
    ///  List of syllables from which the original message will be encrypted
    ///  A null value does not scramlbe the message in any way
    /// </summary>
    [DataField(required: true)]
    public List<string> Syllables = new();

    /// <summary>
    /// Chance of the <see cref="SpecialCharacters"/> after a scrambled syllable
    /// </summary>
    [DataField]
    public float SpecialCharacterChance = 0.5f;

    /// <summary>
    /// Coefficient of how much the length of the scrambled message will differ from the original.
    /// The shorter the syllables length, the higher the accuracy.
    /// </summary>
    [DataField]
    public float ScrambledLengthCoefficient = 1f;

    /// <summary>
    /// Special characters that can be inserted after a scrambled syllable
    /// </summary>
    [DataField]
    public List<SyllablesSpecialCharacter> SpecialCharacters = new();

    private int _inputSeed;
    private bool _capitalize = false;

    public override string ScrambleMessage(string message, int? seed = null)
    {
        if (message == string.Empty ||
            Syllables.Count == 0)
            return message;

        var wordRegex = @"\S+";
        var matches = Regex.Matches(message, wordRegex);
        if (matches.Count <= 0)
            return message;

        var random = IoCManager.Resolve<IRobustRandom>();
        _inputSeed = seed ?? random.Next();
        _capitalize = char.IsUpper(message[0]);

        var result = new StringBuilder();
        foreach (Match m in matches)
        {
            var word = m.Value.ToLower();
            seed = _inputSeed + SharedLanguageSystem.GetSeedFromString(word);
            var scrambledWord = ScrambleWord(m.Value, seed.Value);
            result.Append(scrambledWord);
        }

        var punctuation = ExtractPunctuation(message);
        result.Append(punctuation);

        _capitalize = false;
        return result.ToString().Trim();
    }

    private string ScrambleWord(string word, int seed)
    {
        var random = new System.Random(seed);
        var scrambledMessage = new StringBuilder();
        var scrambledLength = word.Length * ScrambledLengthCoefficient;
        while (scrambledMessage.Length < scrambledLength)
        {
            var curSyllable = random.Pick(Syllables);

            if (_capitalize)
            {
                curSyllable = string.Concat(curSyllable.Substring(0, 1).ToUpper(), curSyllable.AsSpan(1));
                _capitalize = false;
            }
            scrambledMessage.Append(curSyllable);

            if (random.Prob(SpecialCharacterChance))
            {
                var character = GetSpecialCharacter(random);
                if (character != null)
                {
                    scrambledMessage.Append(character.Character);
                    _capitalize = character.Capitalize;
                }
            }
        }

        var result = scrambledMessage.ToString();
        return result;
    }

    /// <summary>
    ///     Takes the last punctuation out of the original post
    ///     (Does not affect the internal punctuation of the sentence)
    /// </summary>
    private static string ExtractPunctuation(string input)
    {
        var punctuationBuilder = new StringBuilder();
        for (var i = input.Length - 1; i >= 0; i--)
        {
            if (char.IsPunctuation(input[i]))
                punctuationBuilder.Insert(0, input[i]);
            else
                break;
        }
        punctuationBuilder.Append(' '); // save whitespace before language tag

        return punctuationBuilder.ToString();
    }

    private SyllablesSpecialCharacter? GetSpecialCharacter(System.Random random)
    {
        var weights = SpecialCharacters.ToDictionary(s => s, s => s.Weight);
        if (weights == null || weights.Count <= 0)
            return null;

        return SharedRandomExtensions.Pick(weights, random);
    }
}

[DataDefinition]
public sealed partial class SyllablesSpecialCharacter
{
    [DataField(required: true)]
    public string Character;

    [DataField]
    public float Weight = 1f;

    [DataField]
    public bool Capitalize = false;
}
