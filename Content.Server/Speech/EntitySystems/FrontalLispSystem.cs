using System.Text.RegularExpressions;
using Content.Server.Speech.Components;
using Robust.Shared.Random; // Corvax-Localization
using Content.Shared.Speech;

namespace Content.Server.Speech.EntitySystems;

public sealed partial class FrontalLispSystem : EntitySystem // ss220 add static regex
{
    [Dependency] private readonly IRobustRandom _random = default!; // Corvax-Localization

    // ss220 add static regex start
    // @formatter:off
    [GeneratedRegex(@"[T]+[Ss]+|[S]+[Cc]+(?=[IiEeYy]+)|[C]+(?=[IiEeYy]+)|[P][Ss]+|([S]+[Tt]+|[T]+)(?=[Ii]+[Oo]+[Uu]*[Nn]*)|[C]+[Hh]+(?=[Ii]*[Ee]*)|[Z]+|[S]+|[X]+(?=[Ee]+)")]
    private static partial Regex RegexUpperTh();

    [GeneratedRegex(@"[t]+[s]+|[s]+[c]+(?=[iey]+)|[c]+(?=[iey]+)|[p][s]+|([s]+[t]+|[t]+)(?=[i]+[o]+[u]*[n]*)|[c]+[h]+(?=[i]*[e]*)|[z]+|[s]+|[x]+(?=[e]+)")]
    private static partial Regex RegexLowerTh();

    [GeneratedRegex(@"[E]+[Xx]+[Cc]*|[X]+")]
    private static partial Regex RegexUpperEcks();

    [GeneratedRegex(@"[e]+[x]+[c]*|[x]+")]
    private static partial Regex RegexLowerEcks();
    // @formatter:on

    [GeneratedRegex(@"с")] private static partial Regex RegexRuS();
    [GeneratedRegex(@"С")] private static partial Regex RegexRuSUpper();
    [GeneratedRegex(@"ч")] private static partial Regex RegexRuCh();
    [GeneratedRegex(@"Ч")] private static partial Regex RegexRuChUpper();
    [GeneratedRegex(@"ц")] private static partial Regex RegexRuTs();
    [GeneratedRegex(@"Ц")] private static partial Regex RegexRuTsUpper();
    [GeneratedRegex(@"\B[т](?![АЕЁИОУЫЭЮЯаеёиоуыэюя])")] private static partial Regex RegexRuT();
    [GeneratedRegex(@"\B[Т](?![АЕЁИОУЫЭЮЯаеёиоуыэюя])")] private static partial Regex RegexRuTUpper();
    [GeneratedRegex(@"з")] private static partial Regex RegexRuZ();
    [GeneratedRegex(@"З")] private static partial Regex RegexRuZUpper();
    // ss220 add static regex end

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FrontalLispComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(EntityUid uid, FrontalLispComponent component, AccentGetEvent args)
    {
        var message = args.Message;

        // ss220 add static regex start
        // handles ts, sc(i|e|y), c(i|e|y), ps, st(io(u|n)), ch(i|e), z, s
        message = RegexUpperTh().Replace(message, "TH");
        message = RegexLowerTh().Replace(message, "th");
        message = RegexUpperEcks().Replace(message, "EKTH");
        message = RegexLowerEcks().Replace(message, "ekth");

        // Corvax-Localization Start
        message = RegexRuS().Replace(message, _ => _random.Prob(0.90f) ? "ш" : "с");
        message = RegexRuSUpper().Replace(message, _ => _random.Prob(0.90f) ? "Ш" : "С");
        message = RegexRuCh().Replace(message, _ => _random.Prob(0.90f) ? "ш" : "ч");
        message = RegexRuChUpper().Replace(message, _ => _random.Prob(0.90f) ? "Ш" : "Ч");
        message = RegexRuTs().Replace(message, _ => _random.Prob(0.90f) ? "ч" : "ц");
        message = RegexRuTsUpper().Replace(message, _ => _random.Prob(0.90f) ? "Ч" : "Ц");
        message = RegexRuT().Replace(message, _ => _random.Prob(0.90f) ? "ч" : "т");
        message = RegexRuTUpper().Replace(message, _ => _random.Prob(0.90f) ? "Ч" : "Т");
        message = RegexRuZ().Replace(message, _ => _random.Prob(0.90f) ? "ж" : "з");
        message = RegexRuZUpper().Replace(message, _ => _random.Prob(0.90f) ? "Ж" : "З");
        // Corvax-Localization End
        // ss220 add static regex end

        args.Message = message;
    }
}
