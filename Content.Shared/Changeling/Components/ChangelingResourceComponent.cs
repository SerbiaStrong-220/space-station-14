// SS220 Changeling
using Content.Shared.FixedPoint;
using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Changeling.Components;

/// <summary>
/// Server-authoritative resources and regenerative stasis state for a changeling.
/// Resource mutations should go through <c>ChangelingResourceSystem</c> rather than
/// assigning these fields directly.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class ChangelingResourceComponent : Component
{
    /// <summary>
    /// Alert displaying the current chemical reserve.
    /// </summary>
    [DataField]
    public ProtoId<AlertPrototype> ChemicalsAlert = "ChangelingChemicals";

    /// <summary>
    /// Chemicals currently available for changeling abilities.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 Chemicals = FixedPoint2.New(75);

    /// <summary>
    /// Maximum amount of chemicals that can be stored.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 MaxChemicals = FixedPoint2.New(75);

    /// <summary>
    /// Owner-visible mirror of the authoritative store balance. Gameplay mutations must only spend through the store.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int EvolutionPoints = 20;

    /// <summary>
    /// Evolution points restored by a full mutation reset.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int MaxEvolutionPoints = 20;

    /// <summary>
    /// Base amount of chemicals regenerated each interval.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 ChemicalRegenerationAmount = FixedPoint2.New(1);

    /// <summary>
    /// Base interval between chemical regeneration ticks.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan ChemicalRegenerationInterval = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Absolute time of the next chemical regeneration tick.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextChemicalRegeneration;

    /// <summary>
    /// Server-only keyed multipliers applied to chemical regeneration.
    /// Multiple active sources multiply together.
    /// </summary>
    [ViewVariables]
    public Dictionary<string, float> ChemicalRegenerationModifiers = new();

    /// <summary>
    /// Action used to enter regenerative stasis.
    /// </summary>
    [DataField]
    public EntProtoId? RegenerativeStasisAction = "ActionChangelingRegenerativeStasis";

    [DataField, AutoNetworkedField]
    public EntityUid? RegenerativeStasisActionEntity;

    /// <summary>
    /// Action used to leave regenerative stasis once its windup has elapsed.
    /// </summary>
    [DataField]
    public EntProtoId? RegenerateAction = "ActionChangelingRegenerate";

    [DataField, AutoNetworkedField]
    public EntityUid? RegenerateActionEntity;

    /// <summary>
    /// Chemical cost paid when entering regenerative stasis.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 RegenerativeStasisChemicalCost = FixedPoint2.New(15);

    /// <summary>
    /// Minimum time that must pass before regeneration can be invoked.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan RegenerativeStasisDuration = TimeSpan.FromSeconds(50);

    [DataField, AutoNetworkedField]
    public bool InRegenerativeStasis;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan? CanRegenerateAt;

    /// <summary>
    /// Set when recovery has been made permanently impossible, such as by decapitation or gibbing.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool RegenerationPermanentlyBlocked;

    public override bool SendOnlyToOwner => true;
}
