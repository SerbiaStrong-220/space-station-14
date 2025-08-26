using Robust.Shared.Serialization;

namespace Content.Shared.SS220.AntiCheat.FOV;

[Serializable, NetSerializable]
public sealed partial class FovEvent(
    bool fovFromComp,
    bool lightFromComp,
    bool fovFromEye,
    bool lightFromEye) : EntityEventArgs
{
    public bool FovFromComp = fovFromComp;
    public bool LightFromComp = lightFromComp;

    public bool FovFromEye = fovFromEye;
    public bool LightFromEye = lightFromEye;
}
