// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.Language.Systems;

namespace Content.Server.SS220.Investigation;

public static class LanguageMessageExtensions
{
    /// <summary>
    ///     Distinct language ids of the non-empty nodes, in the order they appear in the sentence. Not the
    ///     speaker's selected language: a key prefix can switch language part-way through a line.
    /// </summary>
    /// <param name="fallback">Reported when the message yielded none, normally the speaker's selected language.</param>
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

                // Linear scan: a sentence mixes a handful of languages at most, so a HashSet would cost more.
                if (!result.Contains(id))
                    result.Add(id);
            }
        }

        if (result.Count == 0 && fallback is not null)
            result.Add(fallback);

        return result;
    }
}
