using Content.Server.SS220.RecentlyUsedNarcotics;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.SS220.NarcoticsTest;

namespace Content.Server.SS220.NarcoticsTest;

public sealed class NarcoticsTestSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<NarcoticsTestComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<NarcoticsTestComponent, CheckNarcoticsDoAfterEvent>(OnCheckNarcoticsDoAfter);
        SubscribeLocalEvent<NarcoticsTestComponent, ExaminedEvent>(OnExamined);
    }

    private void OnAfterInteract(Entity<NarcoticsTestComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Target == null || !args.CanReach || !HasComp<HumanoidAppearanceComponent>(args.Target) || ent.Comp.IsUsed)
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager,
            args.User,
            ent.Comp.Delay,
            new CheckNarcoticsDoAfterEvent(),
            ent.Owner,
            args.Target,
            ent.Owner)
        {
            BreakOnDamage = true,
            BlockDuplicate = true,
            CancelDuplicate = true,
            BreakOnDropItem = true,
            BreakOnHandChange = true,
            BreakOnMove = true,
            BreakOnWeightlessMove = true,
            RequireCanInteract = true,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnCheckNarcoticsDoAfter(Entity<NarcoticsTestComponent> ent, ref CheckNarcoticsDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target == null)
            return;

        var usedNarcotics = HasComp<RecentlyUsedNarcoticsComponent>(args.Target.Value);

        _appearance.SetData(ent.Owner, NarcoticsData.Key, usedNarcotics);

        ent.Comp.IsUsed = true;
        ent.Comp.IsPositive = usedNarcotics;
        args.Handled = true;
    }

    private void OnExamined(Entity<NarcoticsTestComponent> ent, ref ExaminedEvent args)
    {
        if (!ent.Comp.IsUsed)
            return;

        var result = ent.Comp.IsPositive
            ? Loc.GetString("disease-swab-narcotics-examined-positive")
            : Loc.GetString("disease-swab-narcotics-examined-negative");

        args.PushMarkup(result);
    }
}
