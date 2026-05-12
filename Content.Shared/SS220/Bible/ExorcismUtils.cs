// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Text.RegularExpressions;

namespace Content.Shared.SS220.Bible;

public static class ExorcismUtils
{
    private static readonly Regex FormattingRegex = new Regex(@"\t|\n|\r", RegexOptions.Compiled);

    public static int GetSanitazedMessageLength(string message)
    {
        return message.AsSpan().Trim().Length;
    }

    public static string SanitazeMessage(string message)
    {
        return FormattingRegex.Replace(message, string.Empty).Trim();
    }
}
