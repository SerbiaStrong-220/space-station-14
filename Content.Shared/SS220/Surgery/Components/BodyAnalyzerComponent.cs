// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Surgery.Components;

/// <summary>
/// This component used to define items which can send target uid for <see cref="BodyAnalyzerBUI"/>
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentPause]
public sealed partial class BodyAnalyzerComponent : Component
{
    /// <summary>
    /// When should the next update be sent for the patient
    /// </summary>
    [DataField]
    [AutoPausedField]
    public TimeSpan NextUpdate = TimeSpan.Zero;

    /// <summary>
    /// The delay between patient health updates
    /// </summary>
    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Which entity has been scanned, for continuous updates
    /// </summary>
    [DataField]
    public EntityUid? ScannedEntity;

    /// <summary>
    /// The maximum range in tiles at which the analyzer can receive continuous updates, a value of null will be infinite range
    /// </summary>
    [DataField]
    public float? MaxScanRange = 2.5f;
}
