// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Server.SS220.SurveillanceCamera;

/// <summary>
/// Raised on a camera after <see cref="Content.Server.SurveillanceCamera.SurveillanceCameraSystem.SetActive"/> applied its state.
/// </summary>
[ByRefEvent]
public readonly record struct SurveillanceCameraActiveChangedEvent(bool Active);
