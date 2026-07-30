// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.Language.Systems;

namespace Content.Server.SS220.Investigation;

/// <summary>
///     Pulls the languages a message was actually spoken in out of a <see cref="LanguageMessage"/>, for
///     <see cref="IInvestigationRecorder.OnChat"/>.
/// </summary>
public static class LanguageMessageExtensions
{
    /// <summary>
    ///     Distinct language prototype ids of the non-empty nodes, in the order they appear in the sentence.
    /// </summary>
    /// <param name="message">The sanitized message, or null for a line that never reached the sanitizer.</param>
    /// <param name="fallback">
    ///     Language to report when the message yielded none — normally the speaker's selected one. Without it a
    ///     line that skipped sanitizing records as having been spoken in no language at all.
    /// </param>
    /// <remarks>
    ///     This is deliberately not the speaker's *selected* language, which is what the admin log records as
    ///     <c>defaultLanguage</c>. A speaker can switch language part-way through a line with a key prefix, and for an
    ///     investigator "half of this sentence was in Codespeak" is precisely the interesting part. The empty nodes
    ///     the sanitizer leaves behind between keys are skipped, or every line would report a language nobody spoke.
    /// </remarks>
    public static List<string> SpokenLanguageIds(this LanguageMessage? message, string? fallback = null)
    {
        var result = new List<string>();

        if (message is not null)
        {
            foreach (var node in message.Nodes)
            {
                if (node.Empty)
                    continue;

                var id = node.LanguageId.Id;

                // Linear scan rather than a set: a sentence mixes at most a handful of languages, and all but the
                // rarest carry exactly one, so a HashSet would cost more to allocate than it ever saves.
                if (!result.Contains(id))
                    result.Add(id);
            }
        }

        if (result.Count == 0 && fallback is not null)
            result.Add(fallback);

        return result;
    }
}
