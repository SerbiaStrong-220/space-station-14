// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.TTS;
using Robust.Shared.Network;

namespace Content.Client.SS220.TTS;

/// <summary>
/// Registers TTS net messages. Net messages must be registered before the connection starts, so it is done from the entry point.
/// </summary>
public sealed partial class TtsNetManager
{
    [Dependency] private IClientNetManager _netManager = default!;

    public event Action<MsgPlayTts>? PlayTtsReceived;

    public void Initialize()
    {
        _netManager.RegisterNetMessage<MsgPlayTts>(msg => PlayTtsReceived?.Invoke(msg));
    }
}
