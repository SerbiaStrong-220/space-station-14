using Content.Server.SS220.RecentlyUsedNarcotics;
using Content.Shared.DoAfter;
using Content.Shared.Forensics.Systems;
using Content.Shared.Examine;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.SS220.Narcotics.NarcoticsTest;

namespace Content.Server.SS220.Narcotics.NarcoticsTest;

public sealed partial class NarcoticsTestSystem : EntitySystem
{
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedForensicsSystem _forensics = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<NarcoticsTestComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<NarcoticsTestComponent, CheckNarcoticsDoAfterEvent>(OnCheckNarcoticsDoAfter);
        SubscribeLocalEvent<NarcoticsTestComponent, ExaminedEvent>(OnExamined);
    }

    private void OnAfterInteract(Entity<NarcoticsTestComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Target == null || !args.CanReach || !HasComp<HumanoidProfileComponent>(args.Target) || ent.Comp.IsUsed)
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

        _forensics.TransferDna(ent.Owner, args.Target.Value);

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
