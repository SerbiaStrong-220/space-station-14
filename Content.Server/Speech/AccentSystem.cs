using System.Text.RegularExpressions;
using Content.Server.Chat.Systems;
using Content.Shared.Speech;

namespace Content.Server.Speech;

public sealed class AccentSystem : EntitySystem
{
    public static readonly Regex SentenceRegex = new(@"(?<=[\.!\?‽])(?![\.!\?‽])", RegexOptions.Compiled);

    public override void Initialize()
    {
        SubscribeLocalEvent<TransformSpeechEvent>(AccentHandler);
    }

    private void AccentHandler(TransformSpeechEvent args)
    {
        // SS220 Mindslave-stop-word begin
        var beforeAccentGetEvent = new BeforeAccentGetEvent(args.Sender, args.Message);
        RaiseLocalEvent(args.Sender, beforeAccentGetEvent, true);
        var accentEvent = new AccentGetEvent(args.Sender, beforeAccentGetEvent.Message);
        // var accentEvent = new AccentGetEvent(args.Sender, args.Message);
        // SS220 Mindslave-stop-word end

        RaiseLocalEvent(args.Sender, accentEvent, true);
        args.Message = accentEvent.Message;
    }
}
