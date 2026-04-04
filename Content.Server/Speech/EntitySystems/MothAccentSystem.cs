using System.Text.RegularExpressions;
using Content.Server.Speech.Components;
using Robust.Shared.Random;
using Content.Shared.Speech;

namespace Content.Server.Speech.EntitySystems;

public sealed partial class MothAccentSystem : EntitySystem // ss220 add static regex
{
    [Dependency] private readonly IRobustRandom _random = default!; // Corvax-Localization

    [GeneratedRegex("z{1,3}")]
    private static partial Regex RegexLowerBuzz();

    [GeneratedRegex("Z{1,3}")]
    private static partial Regex RegexUpperBuzz();

    // ss220 add static regex start
    [GeneratedRegex("ж+", RegexOptions.IgnoreCase)]
    private static partial Regex RegexMothZhLower();

    [GeneratedRegex("Ж+")]
    private static partial Regex RegexMothZhUpper();

    [GeneratedRegex("з+")]
    private static partial Regex RegexMothZLower();

    [GeneratedRegex("З+")]
    private static partial Regex RegexMothZUpper();

    private static readonly string[] ZhSmallReplies = ["жж", "жжж"];
    private static readonly string[] ZhBigReplies = ["ЖЖ", "ЖЖЖ"];
    private static readonly string[] ZSmallReplies = ["зз", "ззз"];
    private static readonly string[] ZBigReplies = ["ЗЗ", "ЗЗЗ"];
    // ss220 add static regex end

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MothAccentComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(EntityUid uid, MothAccentComponent component, AccentGetEvent args)
    {
        var message = args.Message;

        // buzzz
        message = RegexLowerBuzz().Replace(message, "zzz");
        // buZZZ
        message = RegexUpperBuzz().Replace(message, "ZZZ");

        // ss220 add static regex start
        // Corvax-Localization-Start
        message = RegexMothZhLower().Replace(message, _ => _random.Pick(ZhSmallReplies));
        message = RegexMothZhUpper().Replace(message, _ => _random.Pick(ZhBigReplies));
        message = RegexMothZLower().Replace(message, _ => _random.Pick(ZSmallReplies));
        message = RegexMothZUpper().Replace(message, _ => _random.Pick(ZBigReplies));
        // Corvax-Localization-End
        // ss220 add static regex end

        args.Message = message;
    }
}
