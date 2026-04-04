using System.Text.RegularExpressions;
using Content.Server.Speech.Components;
using Robust.Shared.Random;
using Content.Shared.Speech;

namespace Content.Server.Speech.EntitySystems;

public sealed partial class LizardAccentSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!; // Corvax-Localization

    [GeneratedRegex("s+")] private static partial Regex RegexLowerS();
    [GeneratedRegex("S+")] private static partial Regex RegexUpperS();
    [GeneratedRegex(@"(\w)x")] private static partial Regex RegexInternalX();
    [GeneratedRegex(@"\bx([\-|r|R]|\b)")] private static partial Regex RegexLowerEndX();
    [GeneratedRegex(@"\bX([\-|r|R]|\b)")] private static partial Regex RegexUpperEndX();

    [GeneratedRegex("с+")] private static partial Regex RegexRuS();
    [GeneratedRegex("С+")] private static partial Regex RegexRuSUpper();
    [GeneratedRegex("з+")] private static partial Regex RegexRuZ();
    [GeneratedRegex("З+")] private static partial Regex RegexRuZUpper();
    [GeneratedRegex("ш+")] private static partial Regex RegexRuSh();
    [GeneratedRegex("Ш+")] private static partial Regex RegexRuShUpper();
    [GeneratedRegex("ч+")] private static partial Regex RegexRuCh();
    [GeneratedRegex("Ч+")] private static partial Regex RegexRuChUpper();

    private static readonly string[] SssReplies = { "сс", "ссс" };
    private static readonly string[] SssUpperReplies = { "СС", "ССС" };
    private static readonly string[] ShhhReplies = { "шш", "шшш" };
    private static readonly string[] ShhhUpperReplies = { "ШШ", "ШШШ" };
    private static readonly string[] SchReplies = { "щщ", "щщщ" };
    private static readonly string[] SchUpperReplies = { "ЩЩ", "ЩЩЩ" };

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LizardAccentComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(EntityUid uid, LizardAccentComponent component, AccentGetEvent args)
    {
        var message = args.Message;

        // hissss
        message = RegexLowerS().Replace(message, "sss");
        // hiSSS
        message = RegexUpperS().Replace(message, "SSS");
        // ekssit
        message = RegexInternalX().Replace(message, m => $"{m.Groups[1].Value}kss");
        // ecks
        message = RegexLowerEndX().Replace(message, m => $"ecks{m.Groups[1].Value}");
        // eckS
        message = RegexUpperEndX().Replace(message, m => $"ECKS{m.Groups[1].Value}");

        // Corvax-Localization-Start
        message = RegexRuS().Replace(message, _ => _random.Pick(SssReplies));
        message = RegexRuSUpper().Replace(message, _ => _random.Pick(SssUpperReplies));

        message = RegexRuZ().Replace(message, _ => _random.Pick(SssReplies));
        message = RegexRuZUpper().Replace(message, _ => _random.Pick(SssUpperReplies));

        message = RegexRuSh().Replace(message, _ => _random.Pick(ShhhReplies));
        message = RegexRuShUpper().Replace(message, _ => _random.Pick(ShhhUpperReplies));

        message = RegexRuCh().Replace(message, _ => _random.Pick(SchReplies));
        message = RegexRuChUpper().Replace(message, _ => _random.Pick(SchUpperReplies));
        // Corvax-Localization-End
        args.Message = message;
    }
}
