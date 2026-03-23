using Robust.Shared.Configuration;

namespace Content.Shared.SS220.CCVars;

public sealed partial class CCVars220
{
    /// <summary>
    /// Master switch for receiving tts
    /// </summary>
    public static readonly CVarDef<bool> RecieveTTS =
        CVarDef.Create("tts.receive_tts", true, CVar.CLIENT | CVar.REPLICATED | CVar.ARCHIVE);

    /// <summary>
    /// Master switch for receiving tts
    /// </summary>
    public static readonly CVarDef<bool> PlayDifferentRadioTogether =
        CVarDef.Create("tts.play_different_radio_together", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Master switch for receiving tts
    /// </summary>
    public static readonly CVarDef<bool> PlayDifferentTalkingTogether =
        CVarDef.Create("tts.play_different_talk_together", true, CVar.CLIENTONLY | CVar.ARCHIVE);


    public static readonly CVarDef<bool> UseFFMpegProcessing =
        CVarDef.Create("tts.use_ffmpeg_processing", true, CVar.SERVERONLY | CVar.ARCHIVE);

    /// <summary>
    /// Maximum entity capacity for tts queue
    /// </summary>
    public static readonly CVarDef<int> MaxQueuedPerEntity =
        CVarDef.Create("tts.max_queued_entity", 20, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Maximum of queued tts entities
    /// </summary>
    public static readonly CVarDef<int> MaxEntitiesQueued =
        CVarDef.Create("tts.max_entities_queued", 30, CVar.CLIENTONLY | CVar.ARCHIVE);

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
}
