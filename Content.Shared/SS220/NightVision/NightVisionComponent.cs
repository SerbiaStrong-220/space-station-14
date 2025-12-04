using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.NightVision;

[RegisterComponent]
[NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class NightVisionComponent : Component
{
    [DataField]
    [AutoNetworkedField]
    public bool Enabled;

    [DataField]
    public float MinLight = 0.0f;

    [DataField]
    public float BrightThreshold = 0.0f;

    [DataField]
    public float BrightBoost = 60f;

    [DataField]
    public float Gamma = 1.2f;

    [DataField]
    public float NoiseAmount = 0.01f;

    [DataField]
    public float MinLightAfterTargetOverlay = 0.013f;

    [DataField]
    public EntProtoId Action = "ActionToggleNightVision";

    [AutoNetworkedField]
    public EntityUid? ActionEntity;
}
