// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Audio;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.CultYogg.Altar;

[RegisterComponent]
public sealed partial class CultYoggAltarComponent : Component
{
    /// <summary>
    /// Delaying sacrifice notifications to avoid spam
    /// </summary>
    [DataField]
    public TimeSpan AnnounceDelay = TimeSpan.FromSeconds(5);

    /// <summary>
    /// The time when the announcement of the sacrifice will take place
    /// </summary>
    public TimeSpan? AnnounceTime;

    /// <summary>
    /// Sacrifice alert sound for crew members
    /// </summary>
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Misc/notice1.ogg");

    /// <summary>
    /// The time during which the DoAfter sacrifice will take place
    /// </summary>
    [DataField]
    public TimeSpan RitualTime = TimeSpan.FromSeconds(185);

    /// <summary>
    /// A mark that it has been used and cannot be used for another sacrifice.
    /// </summary>
    public bool Used;


    [Serializable, NetSerializable]
    public enum CultYoggAltarVisuals
    {
        Sacrificed,
    }
}
