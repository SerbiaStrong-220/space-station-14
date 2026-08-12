using Robust.Shared.Configuration;

namespace Content.Shared.SS220.CCVars;

public sealed partial class CCVars220
{
    /// <summary>
    /// Enables the entire TTS request handle system.
    /// </summary>
    public static readonly CVarDef<bool> TtsEnabled =
        CVarDef.Create("tts.enabled", true, CVar.SERVER | CVar.REPLICATED | CVar.NOTIFY);

    #region NTTS
    /// <summary>
    /// Enables the NTTS provider.
    /// </summary>
    public static readonly CVarDef<bool> TtsNTTSEnabled =
        CVarDef.Create("tts.ntts.enabled", false, CVar.SERVER | CVar.REPLICATED | CVar.NOTIFY);

    public static readonly CVarDef<string> TtsNTTSApiUrl =
        CVarDef.Create("tts.ntts.api_url", "", CVar.SERVERONLY);

    /// <summary>
    /// Maximum number of cached responses for the NTTS provider.
    /// </summary>
    public static readonly CVarDef<int> TtsNTTSMaxCache =
        CVarDef.Create("tts.ntts.max_cache", 250, CVar.SERVERONLY | CVar.ARCHIVE);
    #endregion

    #region Silero
    /// <summary>
    /// Whether the Silero TTS provider is enabled
    /// </summary>
    public static readonly CVarDef<bool> TtsSileroEnabled =
         CVarDef.Create("tts.silero.enabled", true, CVar.SERVER | CVar.REPLICATED | CVar.NOTIFY);

    public static readonly CVarDef<string> TtsSileroApiUrl =
        CVarDef.Create("tts.silero.api_url", "", CVar.SERVERONLY);

    public static readonly CVarDef<string> TtsSileroApiToken =
        CVarDef.Create("tts.silero.api_token", "", CVar.SERVERONLY | CVar.CONFIDENTIAL);

    /// <summary>
    /// Maximum number of cached responses for the Silero provider.
    /// </summary>
    public static readonly CVarDef<int> TtsSileroMaxCache =
        CVarDef.Create("tts.silero.max_cache", 250, CVar.SERVERONLY | CVar.ARCHIVE);
    #endregion

    /// <summary>
    /// TTS request timeout in seconds.
    /// </summary>
    public static readonly CVarDef<float> TtsRequestTimeout =
        CVarDef.Create("tts.timeout", 5f, CVar.SERVERONLY | CVar.ARCHIVE);

    /// <summary>
    /// Whether to use FFmpeg for TTS-audio processing.
    /// </summary>
    public static readonly CVarDef<bool> TtsUseFFMpegProcessing =
        CVarDef.Create("tts.use_ffmpeg_processing", true, CVar.SERVERONLY | CVar.ARCHIVE);

    /// <summary>
    /// Defines how long messages can be processed into audio by tts
    /// </summary>
    public static readonly CVarDef<int> MaxCharInTTSMessage =
        CVarDef.Create("tts.max_char_message", 100 * 2, CVar.SERVERONLY | CVar.ARCHIVE);

    /// <summary>
    /// Defines how long messages can be processed into audio by tts
    /// </summary>
    public static readonly CVarDef<int> MaxCharInTTSAnnounceMessage =
        CVarDef.Create("tts.max_char_announce_message", 100 * 4, CVar.SERVERONLY | CVar.ARCHIVE);

    #region ClientSettings
    /// <summary>
    /// Default volume setting of TTS sound
    /// </summary>
    public static readonly CVarDef<float> TtsVolume =
        CVarDef.Create("tts.volume", 0.50f, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Default volume setting of TTS Radio sound
    /// </summary>
    public static readonly CVarDef<float> TtsRadioVolume =
        CVarDef.Create("tts.radio_volume", 0.50f, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Default volume setting of TTS Announce sound
    /// </summary>
    public static readonly CVarDef<float> TtsAnnounceVolume =
        CVarDef.Create("tts.announce_volume", 0.50f, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Master switch for receiving tts
    /// </summary>
    public static readonly CVarDef<bool> RecieveTts =
        CVarDef.Create("tts.receive_tts", true, CVar.CLIENT | CVar.REPLICATED | CVar.ARCHIVE);

    /// <summary>
    /// Boolean for queueing tts with different radio channels together or sequentially
    /// </summary>
    public static readonly CVarDef<bool> TtsPlayDifferentRadioTogether =
        CVarDef.Create("tts.play_different_radio_together", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Boolean for queueing tts with different source in speech together or sequentially
    /// </summary>
    public static readonly CVarDef<bool> TtsPlayDifferentTalkingTogether =
        CVarDef.Create("tts.play_different_talk_together", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Boolean for queueing tts with different radio channels and speakers together or sequentially
    /// </summary>
    public static readonly CVarDef<bool> TtsPlayDifferentRadioSourcesTogether =
        CVarDef.Create("tts.play_different_radio_sources_together", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Maximum number of TtsPlayRequest's in one queue.
    /// </summary>
    public static readonly CVarDef<int> TtsPlayQueueSizeLimit =
        CVarDef.Create("tts.play_queue_size_limit", 20, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Maximum number of different TtsPlayRequest's queues.
    /// </summary>
    public static readonly CVarDef<int> TtsPlayQueuesCountLimit =
        CVarDef.Create("tts.play_queue_count_limit", 30, CVar.CLIENTONLY | CVar.ARCHIVE);
    #endregion
}
