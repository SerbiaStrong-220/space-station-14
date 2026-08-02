// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.CCVars;
using Content.Shared.SS220.TTS;

namespace Content.Client.SS220.TTS;

public sealed partial class TTSSystem : EntitySystem
{
    private bool _playDifferentRadioTogether = true;
    private bool _playDifferentTalkingTogether = true;
    private bool _playDifferentRadioSourcesTogether = true;

    private void InitializeCacheKeyGeneration()
    {
        _cfg.OnValueChanged(CCVars220.PlayDifferentRadioTogether, x => _playDifferentRadioTogether = x, true);
        _cfg.OnValueChanged(CCVars220.PlayDifferentRadioSourcesTogether, x => _playDifferentRadioSourcesTogether = x, true);
        _cfg.OnValueChanged(CCVars220.PlayDifferentTalkingTogether, x => _playDifferentTalkingTogether = x, true);
    }

    private TtsCacheKey GenerateCacheKey(TtsMetadata meta)
    {
        var key = new TtsCacheKey();

        switch (meta.Kind)
        {
            case TtsKind.Say:
            case TtsKind.Whisper:
                if (_playDifferentTalkingTogether && meta.Source != null)
                    key = key.With(meta.Source.Value.ToString());

                break;

            case TtsKind.Radio:
            case TtsKind.Telepathy:
                if (!_playDifferentRadioTogether && meta.ChannelPrototype != null)
                    key = key.With(meta.ChannelPrototype);

                if (_playDifferentRadioSourcesTogether && meta.Source != null)
                    key = key.With(meta.Source.Value.ToString());

                break;

            case TtsKind.VoiceTest:
                key = key.With(nameof(TtsKind.VoiceTest));
                break;
        }

        return key;
    }
}
