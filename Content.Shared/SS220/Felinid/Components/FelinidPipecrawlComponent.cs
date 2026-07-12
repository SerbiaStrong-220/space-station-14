using Content.Shared.Actions.Components;
using Content.Shared.Atmos;
using Content.Shared.Tools;
using Robust.Shared.GameStates;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Felinid.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class FelinidPipecrawlComponent : Component, IGasMixtureHolder
{
    [DataField]
    public EntProtoId<InstantActionComponent> Action = "ActionFelinidPipecrawl";

    [DataField]
    public EntityUid? ActionEntity;

    [DataField]
    public TimeSpan TransitTime = TimeSpan.FromSeconds(1.1);

    [DataField]
    public float VisionRange = 5f;

    [DataField]
    public float ExtractionDelay = 1.5f;

    [DataField]
    public ProtoId<ToolQualityPrototype> ExtractionQuality = "Anchoring";

    [DataField]
    public TimeSpan EnterCooldown = TimeSpan.FromMinutes(2);

    [DataField]
    public TimeSpan ExitCooldown = TimeSpan.FromMinutes(10);

    [DataField]
    public string OuterClothingSlot = "outerClothing";

    [DataField, AutoNetworkedField]
    public bool Active;

    [DataField, AutoNetworkedField]
    public TimeSpan CooldownStartedAt;

    [DataField, AutoNetworkedField]
    public TimeSpan NextEntryAllowed;

    [DataField]
    public GasMixture Air { get; set; } = new(70f);

    public EntityUid? CurrentTube;
    public EntityUid? NextTube;
    public Direction PreviousDirection = Direction.Invalid;
    public Direction TravelDirection = Direction.Invalid;
    public TimeSpan TravelStartedAt;
    public TimeSpan TravelEndsAt;
}
