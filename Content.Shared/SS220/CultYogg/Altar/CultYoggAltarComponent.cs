// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Serialization;

namespace Content.Shared.SS220.CultYogg.Altar;

[RegisterComponent]
public sealed partial class CultYoggAltarComponent : Component
{
    [DataField]
    public int RequiredAmountMiGo = 1;//ToDo after tests set it to 3

    [DataField]
    public float RitualStartRange = 6f;

    [DataField]
    public TimeSpan RutualTime = TimeSpan.FromSeconds(25);

    [DataField]
    public bool Used = false;

    [DataField]
    public string GodEnt = "Nyarlathotep";

    [Serializable, NetSerializable]
    public enum CultYoggAltarVisuals
    {
        Sacrificed,
    }
}
