using Content.Shared.Atmos;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.Hookah.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HookahComponent : Component
{
    [DataField]
    public string SolutionName = "hookah";

    [DataField, AutoNetworkedField]
    public EntityUid? ConnectedHose;

    [DataField, AutoNetworkedField]
    public bool IsLit;

    [DataField]
    public EntProtoId HosePrototype = "HookahHose";

    [DataField]
    public float InhaleAmount = 2f;

    [DataField]
    public float DragDelay = 3f;

    [DataField]
    public Gas ExhaleGasType = Gas.WaterVapor;

    [DataField]
    public float ExhaleMoles = 0.5f;

    [DataField]
    public SpriteSpecifier RopeSprite =
        new SpriteSpecifier.Rsi(
            new ResPath("Objects/Specific/Hookah/hookah_rope.rsi"), "rope");

    [DataField("useSound")]
    public SoundSpecifier UseSound =
        new SoundPathSpecifier("/Audio/Effects/custom_hookah.ogg");
}
