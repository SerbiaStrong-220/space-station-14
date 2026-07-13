using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.StationEvents.Components;

// SS220-event-director-begin
/// <summary>
/// Per-round state and tuning for the station event director.
/// The director is a game rule: it is the single normal source of random mid-round events.
/// </summary>
[RegisterComponent, Access(typeof(StationEventDirectorSystem)), AutoGenerateComponentPause]
public sealed partial class StationEventDirectorComponent : Component
{
    /// <summary>
    /// The complete pool from which the director may buy an event.
    /// </summary>
    [DataField(required: true)]
    public EntityTableSelector ScheduledGameRules = default!;

    [DataField]
    public float StartingBudget = 18f;

    [DataField]
    public float MaximumBudget = 120f;

    [DataField]
    public float BudgetPerMinute = 2.5f;

    [DataField]
    public float RecoveryBudget = 12f;

    [DataField]
    public TimeSpan MinimumEventDelay = TimeSpan.FromMinutes(4);

    [DataField]
    public TimeSpan MaximumEventDelay = TimeSpan.FromMinutes(7);

    [DataField]
    public TimeSpan AlertBackoff = TimeSpan.FromMinutes(5);

    [ViewVariables]
    public float Budget;

    [ViewVariables, AutoPausedField]
    public TimeSpan NextEventTime;

    [ViewVariables, AutoPausedField]
    public TimeSpan LastBudgetUpdate;
}

/// <summary>
/// Lets gameplay systems feed a resolved or escalating situation into the director budget.
/// Positive values make a future event affordable sooner; negative values slow the director down.
/// </summary>
[ByRefEvent]
public record struct StationEventDirectorBudgetEvent(float Amount);
// SS220-event-director-end
