using Content.Shared.Atmos;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.HookahElectric.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HookahElectricComponent : Component
{
    [DataField]
    public string SolutionName = "hookah";

    [DataField, AutoNetworkedField]
    public EntityUid? LeftHose;

    [DataField, AutoNetworkedField]
    public EntityUid? RightHose;

    [DataField, AutoNetworkedField]
    public bool IsOn;

    [DataField]
    public EntProtoId HosePrototype = "HookahElectricHose";

    [DataField]
    public float InhaleAmount = 2f;

    [DataField]
    public float DragDelay = 3f;

    [DataField]
    public Gas ExhaleGasType = Gas.WaterVapor;

    [DataField]
    public float ExhaleMoles = 15f;

    [DataField]
    public SpriteSpecifier RopeSprite =
        new SpriteSpecifier.Rsi(
            new ResPath("SS220/Objects/Specific/HookahElectric/hookah_electric_rope.rsi"), "rope");

    [DataField]
    public SoundSpecifier ToggleOnSound =
        new SoundPathSpecifier("/Audio/Machines/button.ogg");

    [DataField]
    public SoundSpecifier ToggleOffSound =
        new SoundPathSpecifier("/Audio/Machines/button.ogg");

    [DataField("useSound")]
    public SoundSpecifier UseSound =
        new SoundPathSpecifier("/Audio/Effects/custom_hookah.ogg");
}
