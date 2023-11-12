using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.DiscordLink;

[Serializable, NetSerializable]
public sealed class DiscordLinkEuiState : EuiStateBase
{
    public string? LinkKey { get; }

    public DiscordLinkEuiState(string? linkKey)
    {
        LinkKey = linkKey;
    }
}

