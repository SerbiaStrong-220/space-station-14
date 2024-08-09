// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.Utility;

namespace Content.Shared.SS220.Bible
{
    public static class ExorcismUtils
    {
        public static int GetSanitazedMessageLength(string message)
        {
            return message.AsSpan().Trim().Length;
        }

        public static string SanitazeMessage(string message)
        {
            return message.Trim();
        }
    }
}
