// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.Language.Systems;

namespace Content.Server.SS220.Investigation;

public static class LanguageMessageExtensions
{
    /// <summary>Language ids in sentence order. Not the selected language: a %key can switch mid-line.</summary>
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

                if (!result.Contains(id))
                    result.Add(id);
            }
        }

        if (result.Count == 0 && fallback is not null)
            result.Add(fallback);

        return result;
    }
}
