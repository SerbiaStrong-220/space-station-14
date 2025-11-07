// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Globalization;
using System.Linq;
using Content.Shared.SS220.CCVars;
using Content.Shared.SS220.TTS;

namespace Content.Client.SS220.TTS;

// ReSharper disable once InconsistentNaming
public sealed partial class TTSSystem : EntitySystem
{
    private bool _playDifferentRadioTogether = true;
    private bool _playDifferentTalkingTogether = true;

    private void InitializeMetadata()
    {
        _cfg.OnValueChanged(CCVars220.PlayDifferentRadioTogether, x => _playDifferentRadioTogether = x, true);
        _cfg.OnValueChanged(CCVars220.PlayDifferentTalkingTogether, x => _playDifferentTalkingTogether = x, true);
    }

    private void ModifyMetadata(ref TtsMetadata ttsMetadata, EntityUid source)
    {
        switch (ttsMetadata.Kind)
        {
            case TtsKind.Default:
                if (_playDifferentTalkingTogether)
                    ttsMetadata.Subkind = source == EntityUid.FirstUid ? string.Empty : GetNetEntity(source).Id.ToString();
                else
                    ttsMetadata.Subkind = string.Empty;

                break;

            case TtsKind.Whisper:
                if (_playDifferentTalkingTogether)
                    ttsMetadata.Subkind = source == EntityUid.FirstUid ? string.Empty : GetNetEntity(source).Id.ToString();
                else
                    ttsMetadata.Subkind = string.Empty;

                break;

            case TtsKind.Radio:
                if (!_playDifferentRadioTogether)
                    ttsMetadata.Subkind = string.Empty;

                break;
        }
    }
}
