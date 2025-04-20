// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.GameTicking.Events;
using Content.Shared.SS220.Language.Components;
using Content.Shared.SS220.Language;
using Robust.Shared.Random;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.SS220.Language.Systems;
using Robust.Shared.Configuration;
using Content.Shared.SS220.CCVars;
using Content.Shared.Paper;
using Content.Shared.SS220.Paper;

namespace Content.Server.SS220.Language;

public sealed partial class LanguageSystem : SharedLanguageSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly LanguageManager _language = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);
        SubscribeLocalEvent<LanguageComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<LanguageComponent, SendLanguageMessageAttemptEvent>(OnSendLanguageMessageAttemptEvent);

        // Client
        SubscribeNetworkEvent<ClientSelectLanguageEvent>(OnClientSelectLanguage);
        SubscribeNetworkEvent<ClientRequestPaperLanguageNodeInfo>(OnClientRequestPaperNodeInfo);
    }

    private void OnRoundStart(RoundStartingEvent args)
    {
        SetSeed(_random.Next());
        PaperNodes.Clear();
    }

    /// <summary>
    ///     Initializes an entity with a language component,
    ///     either the first language in the LearnedLanguages list into the CurrentLanguage variable
    /// </summary>
    private void OnMapInit(Entity<LanguageComponent> ent, ref MapInitEvent args)
    {
        TrySetLanguage(ent, 0);
    }

    private void OnSendLanguageMessageAttemptEvent(Entity<LanguageComponent> ent, ref SendLanguageMessageAttemptEvent args)
    {
        args.Listener = ent;
    }

    #region Client
    private void OnClientSelectLanguage(ClientSelectLanguageEvent msg, EntitySessionEventArgs args)
    {
        var entity = args.SenderSession.AttachedEntity;
        if (entity == null || !TryComp<LanguageComponent>(entity, out var comp))
            return;

        TrySetLanguage((entity.Value, comp), msg.LanguageId);
    }

    private void UpdateSeed()
    {
        var ev = new UpdateLanguageSeedEvent(Seed);
        RaiseNetworkEvent(ev);
    }
    #endregion
    /// <summary>
    ///     Raises event to receive a response is it possible to send a message in the language
    ///     This is done for the possibility of forwarding
    /// </summary>
    public bool SendLanguageMessageAttempt(EntityUid uid, out EntityUid listener)
    {
        var ev = new SendLanguageMessageAttemptEvent();
        RaiseLocalEvent(uid, ref ev);
        listener = ev.Listener ?? uid;
        return !ev.Cancelled;
    }

    /// <summary>
    ///     Adds languages for <paramref name="target"/> from <paramref name="ent"/>
    /// </summary>
    public void AddLanguagesFromSource(Entity<LanguageComponent> ent, EntityUid target)
    {
        var targetComp = EnsureComp<LanguageComponent>(target);
        foreach (var language in ent.Comp.AvailableLanguages)
        {
            AddLanguage((target, targetComp), language);
        }
    }

    public bool MessageLanguagesLimit(EntityUid source, string message, [NotNullWhen(true)] out string? reason)
    {
        reason = null;
        if (!HasComp<LanguageComponent>(source))
            return false;

        var defaultLanguage = GetSelectedLanguage(source);
        if (defaultLanguage == null)
            return false;

        var languagesLimit = _config.GetCVar(CCVars220.MaxLanguagesInOneMessage);
        var languagesStrings = SplitMessageByLanguages(source, message, defaultLanguage);
        if (languagesStrings.Count > languagesLimit)
        {
            reason = Loc.GetString("language-message-languages-limit", ("limit", languagesLimit));
            return true;
        }

        return false;
    }

    private void SetSeed(int seed)
    {
        Seed = seed;
        UpdateSeed();
    }
}

[ByRefEvent]
public sealed class SendLanguageMessageAttemptEvent() : CancellableEntityEventArgs
{
    public EntityUid? Listener = null;
}

