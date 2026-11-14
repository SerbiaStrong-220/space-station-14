// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Forensics;
using Content.Server.Popups;
using Content.Server.SS220.Forensics.Components;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.SS220.Forensics;
using Content.Shared.Verbs;

namespace Content.Server.SS220.Forensics.Systems;

/// <summary>
/// Collects deposited material evidence or an item's own material reference into a new physical sample.
/// </summary>
public sealed class GloveMicroFiberScannerSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly GloveMicroFiberSystem _gloveMicroFiber = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly MetaDataSystem _metadata = default!;
    [Dependency] private readonly StorageMicroFiberSystem _storageMicroFiber = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GloveMicroFiberScannerComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<GloveMicroFiberScannerComponent, GetVerbsEvent<UtilityVerb>>(OnUtilityVerb);
        SubscribeLocalEvent<GloveMicroFiberScannerComponent, GloveMicroFiberScannerDoAfterEvent>(OnDoAfter);
    }

    private void OnAfterInteract(Entity<GloveMicroFiberScannerComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        if (args.Target == null)
            return;

        // A handled interaction deposits contact evidence; scanning must not contaminate the target.
        StartScan(ent, args.User, args.Target.Value);
    }

    private void OnUtilityVerb(Entity<GloveMicroFiberScannerComponent> ent, ref GetVerbsEvent<UtilityVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var user = args.User;
        var target = args.Target;
        args.Verbs.Add(new UtilityVerb
        {
            Act = () => StartScan(ent, user, target),
            IconEntity = GetNetEntity(ent),
            Text = Loc.GetString("glove-micro-fiber-scanner-verb-text"),
            Message = Loc.GetString("glove-micro-fiber-scanner-verb-message"),
            DoContactInteraction = false,
        });
    }

    private void StartScan(Entity<GloveMicroFiberScannerComponent> ent, EntityUid user, EntityUid target)
    {
        if (!CanScanTarget(ent, target))
        {
            _popup.PopupEntity(Loc.GetString("glove-micro-fiber-scanner-no-own-sample"), ent, user);
            return;
        }

        var doAfterEvent = new GloveMicroFiberScannerDoAfterEvent
        {
            Mode = ent.Comp.Mode,
        };
        if (!_doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, user, ent.Comp.ScanDelay,
            doAfterEvent, ent, target: target, used: ent)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
            DuplicateCondition = DuplicateConditions.SameTool,
            CancelDuplicate = false,
        }))
            return;
    }

    private bool CanScanTarget(Entity<GloveMicroFiberScannerComponent> ent, EntityUid target)
    {
        if (ent.Comp.Mode != GloveMicroFiberScannerMode.OwnSample)
            return true;

        return HasOwnSample(target);
    }

    private bool HasOwnSample(EntityUid target)
    {
        if (_gloveMicroFiber.TryGetCode(target, out _))
            return true;

        return _storageMicroFiber.TryGetTrace(target, out _);
    }

    private void OnDoAfter(Entity<GloveMicroFiberScannerComponent> ent, ref GloveMicroFiberScannerDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (args.Args.Target == null)
            return;

        var target = args.Args.Target.Value;
        if (Deleted(target))
            return;

        if (args.Mode != ent.Comp.Mode)
        {
            _popup.PopupEntity(Loc.GetString("glove-micro-fiber-scanner-mode-changed"), ent, args.User);
            return;
        }

        if (!TryGetSample(target, args.Mode, out var traces, out var storageMicroFibers))
        {
            _popup.PopupEntity(Loc.GetString("glove-micro-fiber-scanner-no-own-sample"), ent, args.User);
            return;
        }

        var swab = Spawn(ent.Comp.SwabPrototype, Transform(args.User).Coordinates);
        if (!TryComp<GloveMicroFiberSwabComponent>(swab, out var sample))
        {
            Log.Error($"Glove trace scanner output {ToPrettyString(swab)} has no GloveMicroFiberSwabComponent.");
            Del(swab);
            return;
        }

        sample.GloveMicroFibers = traces;
        sample.StorageMicroFibers = storageMicroFibers;
        sample.Used = true;
        sample.SourceName = MetaData(target).EntityName;
        _metadata.SetEntityName(swab, Loc.GetString("glove-micro-fiber-swab-label", ("source", sample.SourceName)));
        _hands.PickupOrDrop(args.User, swab, checkActionBlocker: false);
        args.Handled = true;
    }

    private bool TryGetSample(
        EntityUid target,
        GloveMicroFiberScannerMode mode,
        out List<GloveMicroFiberTrace> traces,
        out List<StorageMicroFiberTrace> storageMicroFibers)
    {
        if (mode == GloveMicroFiberScannerMode.SurfaceTraces)
        {
            CollectSurfaceTraces(target, out traces, out storageMicroFibers);
            return true;
        }

        traces = [];
        storageMicroFibers = [];
        if (_gloveMicroFiber.TryGetTrace(target, out var gloveTrace))
            traces = [gloveTrace];

        if (!_storageMicroFiber.TryGetTrace(target, out var fiber))
            return traces.Count != 0;

        storageMicroFibers = [fiber];
        return true;
    }

    private void CollectSurfaceTraces(
        EntityUid target,
        out List<GloveMicroFiberTrace> traces,
        out List<StorageMicroFiberTrace> storageMicroFibers)
    {
        traces = [];
        storageMicroFibers = [];
        if (!TryComp<ForensicsComponent>(target, out var forensics))
            return;

        traces = [.. forensics.GloveMicroFibers];
        storageMicroFibers = [.. forensics.StorageMicroFibers];
    }
}
