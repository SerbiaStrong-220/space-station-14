// Original code licensed under Imperial CLA.
// Copyright holders: orix0689 (discord) and pocchitsu (discord)

// Modified and/or redistributed under SS220 CLA with hosting restrictions.
// Full license text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.Ninja.Objectives.LeaveNoTrace.DataDefinition;

[DataDefinition, Serializable, NetSerializable]
public sealed partial class TextureGlitchParametersData
{
    /// <summary>
    /// Fill threshold at which texture starts to lag
    /// </summary>
    [DataField]
    public float GlitchThreshold = 0.9f;

    /// <summary>
    /// A sprite that will gradually transform as the ninja is revealed.
    /// </summary>
    [DataField]
    public ResPath RevealSpritePath = new("/Textures/SS220/Interface/Misc/ninja/eye.png");

    /// <summary>
    /// Eye glitch params
    /// </summary>
    [DataField]
    public GlitchShaderParametersData Glitch = new()
    {
        ShakePower = 0.02f
    };
}
