// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.Chat.Systems;
using Content.Shared.SS220.TTS;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using System.Diagnostics.CodeAnalysis;

namespace Content.Server.SS220.TTS;

public partial class TtsSystem
{
    private bool TryGetEntitySpeakerData(EntityUid uid, [NotNullWhen(true)] out TtsEntitySpeakerData? data)
    {
        data = null;
        if (!TryGetAvailableVoice(uid, out var voice))
            return false;

        data = new TtsEntitySpeakerData()
        {
            Speaker = uid,
            NetSpeaker = GetNetEntity(uid),
            Voice = voice
        };
        return true;
    }
}

public struct TtsEntitySpeakerData
{
    public required EntityUid Speaker;
    public required NetEntity NetSpeaker;
    public required TtsVoicePrototype Voice;
}

public interface ITtsRequest { }

public interface ITtsSpokeRequest : ITtsRequest
{
    TtsEntitySpeakerData SpeakerData { get; set; }
    string Text { get; set; }
    HashSet<ICommonSession> Receivers { get; set; }
}

public struct TtsSayRequest() : ITtsSpokeRequest
{
    public required TtsEntitySpeakerData SpeakerData { get; set; }
    public required string Text { get; set; }
    public required HashSet<ICommonSession> Receivers { get; set; }
}

public struct TtsWhisperRequest() : ITtsSpokeRequest
{
    public required TtsEntitySpeakerData SpeakerData { get; set; }
    public required string Text { get; set; }
    public required string ObfuscatedText;
    public required HashSet<ICommonSession> Receivers { get; set; }
}

public struct TtsRadioRequest() : ITtsRequest
{
    public required TtsEntitySpeakerData SpeakerData;
    public required string Text;
    public required string ChannelPrototype;
    public required List<RadioEventReceiver> Receivers;
}

public struct TtsAnnouncementRequest() : ITtsRequest
{
    public SoundSpecifier? AnnouncementSound;
    public string? Text;
    public TtsVoicePrototype? Voice;
    public required HashSet<ICommonSession> Receivers;
}

public struct TtsTelepathyRequest : ITtsRequest
{
    public required TtsEntitySpeakerData SpeakerData;
    public required string Text;
    public string? ChannelPrototype;
    public required HashSet<ICommonSession> Receivers;
}

public struct TtsVoiceTestRequest : ITtsRequest
{
    public required TtsVoicePrototype Voice;
    public required string Text;
    public required HashSet<ICommonSession> Receivers;
}
