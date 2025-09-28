// Original code licensed under Imperial CLA.
// Copyright holders: orix0689 (discord) and pocchitsu (discord)

// Modified and/or redistributed under SS220 CLA with hosting restrictions.
// Full license text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Ninja.Objectives.LeaveNoTrace.DataDefinition;

[DataDefinition, Serializable, NetSerializable]
public sealed partial class GlitchShaderParametersData
{
    /// <summary>
    /// Power of glitch shake
    /// </summary>
    [DataField]
    public float ShakePower = 0.02f;

    /// <summary>
    /// How often will it lag?
    /// <para>
    /// 0.0 - no lag. 1.0 - lag every frame
    /// </para>
    /// </summary>
    [DataField]
    public float SnakeRate = 1.0f;

    /// <summary>
    /// Speed of lag shake
    /// </summary>
    [DataField]
    public float SnakeSpeed = 3.0f;

    /// <summary>
    /// Responsible for dividing the image into color parts. Like VHS tapes
    /// </summary>
    [DataField]
    public float ShakeBlockSize = 100.5f;

    /// <summary>
    /// How much should we separate colors from each other?
    /// </summary>
    [DataField]
    public float SnakeColorRate = 0.1f;
}
