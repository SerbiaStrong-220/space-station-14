using Robust.Shared.Configuration;

namespace Content.Shared.SS220.CCVars;

public sealed partial class CCVars220
{
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
