// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.TTS;
using Robust.Shared.Network;

namespace Content.Client.SS220.TTS;

public sealed class TTSManager
{
    [Dependency] private readonly IClientNetManager _netManager = default!;

    public event Action<MsgPlayTts>? PlayTtsReceived;
    public event Action<MsgPlayAnnounceTts>? PlayAnnounceTtsReceived;

    public void Initialize()
    {
        _netManager.RegisterNetMessage<MsgPlayTts>(x => PlayTtsReceived?.Invoke(x));
        _netManager.RegisterNetMessage<MsgPlayAnnounceTts>(x => PlayAnnounceTtsReceived?.Invoke(x));
    }
}
