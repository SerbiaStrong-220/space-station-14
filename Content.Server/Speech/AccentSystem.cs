using System.Text.RegularExpressions;
using Content.Server.Chat;
using Content.Server.Chat.Systems;

namespace Content.Server.Speech
{
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

    public sealed class AccentGetEvent : EntityEventArgs
    {
        /// <summary>
        ///     The entity to apply the accent to.
        /// </summary>
        public EntityUid Entity { get; }

        /// <summary>
        ///     The message to apply the accent transformation to.
        ///     Modify this to apply the accent.
        /// </summary>
        public string Message { get; set; }

        public AccentGetEvent(EntityUid entity, string message)
        {
            Entity = entity;
            Message = message;
        }
    }
    // SS220 Mindslave-stop-word begin
    /// <summary>
    /// Raised before common accent event. Used for complex replacement.
    /// </summary>
    public sealed class BeforeAccentGetEvent : EntityEventArgs
    {
        /// <summary>
        ///     The entity to apply the accent to.
        /// </summary>
        public EntityUid Entity { get; }

        /// <summary>
        ///     The message to apply the accent transformation to.
        ///     Modify this to apply the accent.
        /// </summary>
        public string Message { get; set; }

        public BeforeAccentGetEvent(EntityUid entity, string message)
        {
            Entity = entity;
            Message = message;
        }
    }
    // SS220 Mindslave-stop-word end
}
