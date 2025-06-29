// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Text.RegularExpressions;
using Robust.Shared.Random;
using Content.Server.SS220.Speech.Components;
using Content.Server.Speech;

namespace Content.Server.SS220.Speech.EntitySystems;

public sealed class VoxAccentSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    private static readonly Regex RegexLowerK = new("k+");
    private static readonly Regex RegexUpperK = new("K+");
    private static readonly Regex RegexRuLowerK = new("к+");
    private static readonly Regex RegexRuUpperK = new("К+");
    private static readonly Regex RegexLowerCH = new("ch+");
    private static readonly Regex RegexUpperCH = new("CH+");
    private static readonly Regex RegexRuLowerCH = new("ч+");
    private static readonly Regex RegexRuUpperCH = new("Ч+");
    
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<VoxAccentComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(Entity<VoxAccentComponent> entity, ref AccentGetEvent args)
    {
        var message = args.Message;

        // k into kk or kek
        message = RegexLowerK.Replace(message, _random.Pick(new List<string>() { "kk", "kek" }));
        // K into KK or KEK
        message = RegexUpperK.Replace(message, _random.Pick(new List<string>() { "KK", "KEK" }));
        // к в кк или кик
        message = RegexRuLowerK.Replace(message, _random.Pick(new List<string>() { "кк", "кик" }));
        // К в КК или КИК
        message = RegexRuUpperK.Replace(message, _random.Pick(new List<string>() { "КК", "КИК" }));

        // ch into chch or chech
        message = RegexLowerCH.Replace(message, _random.Pick(new List<string>() { "chch", "chech" }));
        // CH into CHCH or CHECH
        message = RegexUpperCH.Replace(message, _random.Pick(new List<string>() { "CHCH", "CHECH" }));
        // ч в чч или чич
        message = RegexRuLowerCH.Replace(message, _random.Pick(new List<string>() { "чч", "чич" }));
        // Ч в ЧЧ или ЧИЧ
        message = RegexRuUpperCH.Replace(message, _random.Pick(new List<string>() { "ЧЧ", "ЧИЧ" }));

        args.Message = message;
    }
}
