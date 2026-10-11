using Content.Shared.Actions.Components;
using Content.Shared.Atmos;
using Content.Shared.Tools;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Felinid.Components;

/// <summary>
/// Allows an entity to travel through the disposal network and stores its pipe-specific state.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class DisposalPipeCrawlerComponent : Component
{
    [DataField]
    public EntProtoId<InstantActionComponent> Action = "ActionDisposalPipeCrawler";

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

    [DataField, AutoNetworkedField]
    public bool InsidePipe;

    [DataField, AutoNetworkedField]
    public TimeSpan CooldownStartedAt;

    [DataField, AutoNetworkedField]
    public TimeSpan NextEntryAllowed;

    /// <summary>
    /// Air carried from the disposal unit while the crawler is isolated from the atmosphere.
    /// </summary>
    [DataField]
    public GasMixture Air { get; set; } = new(70f);

    /// <summary>
    /// Tube the crawler is in. Networked so the client's interaction and attack checks match the server.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? CurrentTube;

    // Travel progress is server-only: pipe movement is not predicted.
    public EntityUid? NextTube;
    public Direction PreviousDirection = Direction.Invalid;
    public Direction TravelDirection = Direction.Invalid;
    public TimeSpan TravelStartedAt;
    public TimeSpan TravelEndsAt;

    /// <summary>
    /// Item the crawler just took off inside a pipe, allowed to finish its way into the crawler's hand.
    /// </summary>
    public EntityUid? UnequippedItem;
}
