// SS220 Changeling
using Content.Shared.Actions;
using Content.Shared.FixedPoint;

namespace Content.Shared.Changeling;

/// <summary>
/// Shared request to atomically spend changeling chemicals on the authoritative server.
/// It starts cancelled so an absent resource handler can never grant a free ability use.
/// </summary>
[ByRefEvent]
public record struct ChangelingChemicalSpendAttemptEvent
{
    public readonly FixedPoint2 Amount;
    public bool Cancelled = true;
    public bool Handled;

    public ChangelingChemicalSpendAttemptEvent(FixedPoint2 amount)
    {
        Amount = amount;
    }
}

/// <summary>
/// Shared request to add chemicals through the authoritative resource system.
/// </summary>
[ByRefEvent]
public record struct ChangelingAddChemicalsEvent
{
    public readonly FixedPoint2 Amount;
    public FixedPoint2 AmountAdded;
    public bool Handled;

    public ChangelingAddChemicalsEvent(FixedPoint2 amount)
    {
        Amount = amount;
    }
}

/// <summary>
/// Shared request to remove learned mutations and restore the full evolution budget.
/// </summary>
[ByRefEvent]
public record struct ChangelingResetEvolutionEvent
{
    public bool Handled;
    public bool Succeeded;
}

[ByRefEvent]
public readonly record struct ChangelingChemicalsChangedEvent(FixedPoint2 OldValue, FixedPoint2 NewValue);

[ByRefEvent]
public readonly record struct ChangelingEvolutionPointsChangedEvent(int OldValue, int NewValue);

/// <summary>
/// Raised before evolution points are restored. Mutation owners must use this event to remove
/// purchased actions, components, and active effects. The resource system restores the points
/// to <see cref="TargetPoints"/> after all synchronous handlers finish.
/// </summary>
[ByRefEvent]
public readonly record struct ChangelingEvolutionResetEvent(int PreviousPoints, int TargetPoints);

/// <summary>
/// Raised by the canonical resource lifecycle handler after changeling resource actions and alerts are removed.
/// Mutation systems use this to tear down effects without subscribing to the exclusive component lifecycle event.
/// </summary>
[ByRefEvent]
public readonly record struct ChangelingResourceRemovedEvent(bool EntityTerminating);

public sealed partial class ChangelingRegenerativeStasisActionEvent : InstantActionEvent;

public sealed partial class ChangelingRegenerateActionEvent : InstantActionEvent;
