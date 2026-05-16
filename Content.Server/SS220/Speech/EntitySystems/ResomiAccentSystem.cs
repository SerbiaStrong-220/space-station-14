// EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Text.RegularExpressions;
using Robust.Shared.Random;
using Content.Server.SS220.Speech.Components;
using Content.Shared.Speech;

namespace Content.Server.SS220.Speech.EntitySystems;

public sealed class ResomiAccentSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    // Double all z/Z
    private static readonly Regex RegexLowerZ = new("z+");
    private static readonly Regex RegexUpperZ = new("Z+");
    private static readonly Regex RegexRuLowerZ = new("з+");
    private static readonly Regex RegexRuUpperZ = new("З+");

    // Insert 'sh' after 'ch'
    private static readonly Regex RegexLowerCH = new("ch");
    private static readonly Regex RegexUpperCH = new("CH");
    private static readonly Regex RegexRuLowerCH = new("ч");
    private static readonly Regex RegexRuUpperCH = new("Ч");

    // Insert 'sh' after 'c'
    private static readonly Regex RegexLowerC = new("c");
    private static readonly Regex RegexUpperC = new("C");
    private static readonly Regex RegexRuLowerC = new("ц");
    private static readonly Regex RegexRuUpperC = new("Ц");

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ResomiAccentComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(Entity<ResomiAccentComponent> entity, ref AccentGetEvent args)
    {
        var message = args.Message;

        // Double all 'z'/'Z'
        message = RegexLowerZ.Replace(message, _random.Pick(new List<string>() { "zz", "zzz" }));
        message = RegexUpperZ.Replace(message, _random.Pick(new List<string>() { "ZZ", "ZZZ" }));
        message = RegexRuLowerZ.Replace(message, _random.Pick(new List<string>() { "зз", "ззз" }));
        message = RegexRuUpperZ.Replace(message, _random.Pick(new List<string>() { "ЗЗ", "ЗЗЗ" }));

        // "ch" into "chsh"
        message = RegexLowerCH.Replace(message, "chsh");
        message = RegexUpperCH.Replace(message, "CHSH");
        message = RegexRuLowerCH.Replace(message, "чщ");
        message = RegexRuUpperCH.Replace(message, "ЧЩ");

        // "c" into "csh"
        message = RegexLowerC.Replace(message, "csh");
        message = RegexUpperC.Replace(message, "CSH");
        message = RegexRuLowerC.Replace(message, "цщ");
        message = RegexRuUpperC.Replace(message, "ЦЩ");

        args.Message = message;
    }
}