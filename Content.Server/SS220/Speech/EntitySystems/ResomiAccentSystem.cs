// EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Text.RegularExpressions;
using Robust.Shared.Random;
using Content.Server.SS220.Speech.Components;
using Content.Shared.Speech;

namespace Content.Server.SS220.Speech.EntitySystems;

public sealed partial class ResomiAccentSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    // Double all z/Z
    [GeneratedRegex("z+")] private static partial Regex LowerZRegex();
    private static readonly string[] LowerZReplacements = ["zz", "zzz"];
    [GeneratedRegex("Z+")] private static partial Regex UpperZRegex();
    private static readonly string[] UpperZReplacements = ["ZZ", "ZZZ"];
    [GeneratedRegex("з+")] private static partial Regex RuLowerZRegex();
    private static readonly string[] RuLowerZReplacements = ["зз", "ззз"];
    [GeneratedRegex("З+")] private static partial Regex RuUpperZRegex();
    private static readonly string[] RuUpperZReplacements = ["ЗЗ", "ЗЗЗ"];

    // Insert 'sh' after 'ch'
    [GeneratedRegex("ch")] private static partial Regex LowerCHRegex();
    private const string LowerCHReplacement = "chsh";
    [GeneratedRegex("CH")] private static partial Regex UpperCHRegex();
    private const string UpperCHReplacement = "CHSH";
    [GeneratedRegex("ч")] private static partial Regex RuLowerCHRegex();
    private const string RuLowerCHReplacement = "чщ";
    [GeneratedRegex("Ч")] private static partial Regex RuUpperCHRegex();
    private const string RuUpperCHReplacement = "ЧЩ";

    // Insert 'sh' after 'c'
    [GeneratedRegex("c")] private static partial Regex LowerCRegex();
    private const string LowerCReplacement = "csh";
    [GeneratedRegex("C")] private static partial Regex UpperCRegex();
    private const string UpperCReplacement = "CSH";
    [GeneratedRegex("ц")] private static partial Regex RuLowerCRegex();
    private const string RuLowerCReplacement = "цщ";
    [GeneratedRegex("Ц")] private static partial Regex RuUpperCRegex();
    private const string RuUpperCReplacement = "ЦЩ";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ResomiAccentComponent, AccentGetEvent>(OnGetAccent);
    }

    private void OnGetAccent(Entity<ResomiAccentComponent> entity, ref AccentGetEvent args)
    {
        var message = args.Message;

        // Double all 'z'/'Z'
        message = LowerZRegex().Replace(message, _ => _random.Pick(LowerZReplacements));
        message = UpperZRegex().Replace(message, _ => _random.Pick(UpperZReplacements));
        message = RuLowerZRegex().Replace(message, _ => _random.Pick(RuLowerZReplacements));
        message = RuUpperZRegex().Replace(message, _ => _random.Pick(RuUpperZReplacements));

        // "ch" into "chsh"
        message = LowerCHRegex().Replace(message, LowerCHReplacement);
        message = UpperCHRegex().Replace(message, UpperCHReplacement);
        message = RuLowerCHRegex().Replace(message, RuLowerCHReplacement);
        message = RuUpperCHRegex().Replace(message, RuUpperCHReplacement);

        // "c" into "csh"
        message = LowerCRegex().Replace(message, LowerCReplacement);
        message = UpperCRegex().Replace(message, UpperCReplacement);
        message = RuLowerCRegex().Replace(message, RuLowerCReplacement);
        message = RuUpperCRegex().Replace(message, RuUpperCReplacement);

        args.Message = message;
    }
}
