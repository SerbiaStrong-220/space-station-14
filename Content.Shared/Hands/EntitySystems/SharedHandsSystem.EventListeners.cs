using Content.Shared.Hands.Components;
using Content.Shared.Stunnable;

namespace Content.Shared.Hands.EntitySystems;

/// <summary>
/// This is for events that don't affect normal hand functions but do care about hands.
/// </summary>
public abstract partial class SharedHandsSystem
{
    private const float SpeedWithZeroFreeHands = 0.85f;
    private const float SpeedWithAllFreeHands = 1.15f;

    private void InitializeEventListeners()
    {
        SubscribeLocalEvent<HandsComponent, GetStandUpTimeEvent>(OnStandupArgs);
        SubscribeLocalEvent<HandsComponent, KnockedDownRefreshEvent>(OnKnockedDownRefresh);
    }

    /// <summary>
    /// Reduces the time it takes to stand up based on the number of hands we have available.
    /// </summary>
    private void OnStandupArgs(Entity<HandsComponent> ent, ref GetStandUpTimeEvent time)
    {
        if (!HasComp<KnockedDownComponent>(ent))
            return;

        var hands = GetEmptyHandCount(ent.Owner);

        if (hands == 0)
            return;

        time.DoAfterTime *= (float)ent.Comp.Count / (hands + ent.Comp.Count);
    }

    private void OnKnockedDownRefresh(Entity<HandsComponent> ent, ref KnockedDownRefreshEvent args)
    {
        float freeHands = CountFreeHands(ent.AsNullable()); // SS220-legs-add
        float totalHands = GetHandCount(ent.AsNullable()); // SS220-legs-add

        // SS220-legs-add-begin
        var freeHandsModifiers = totalHands == 0 ? 0f : freeHands / totalHands;
        args.SpeedModifier *= SpeedWithZeroFreeHands + (SpeedWithAllFreeHands - SpeedWithZeroFreeHands) * freeHandsModifiers;
        // SS220-legs-add-end
    }
}
