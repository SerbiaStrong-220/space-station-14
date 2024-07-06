using System.Text.RegularExpressions;
using Robust.Shared.Random;
using Content.Server.SS220.Speech.Components;
using Content.Server.Speech;

namespace Content.Server.SS220.Speech.EntitySystems;

public sealed class VulpkaninAccentSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    private static readonly Regex RegexLowerR = new("r+");
    private static readonly Regex RegexUpperR = new("R+");
    private static readonly Regex RegexRuLowerR = new("р+");
    private static readonly Regex RegexRuUpperR = new("Р+");
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<VulpkaninAccentComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(EntityUid uid, VulpkaninAccentComponent component, AccentGetEvent args)
    {
        var message = args.Message;

        // r into rr or rrr
        message = RegexLowerR.Replace(message, _random.Pick(new List<string>() { "rr", "rrr" }));
        // R into RR or RRR
        message = RegexUpperR.Replace(message, _random.Pick(new List<string>() { "RR", "RRR" }));
        // р в рр или ррр
        message = RegexRuLowerR.Replace(message, _random.Pick(new List<string>() { "рр", "ррр" }));
        // Р в РР или РРР
        message = RegexRuUpperR.Replace(message, _random.Pick(new List<string>() { "РР", "РРР" }));

        args.Message = message;
    }
}
