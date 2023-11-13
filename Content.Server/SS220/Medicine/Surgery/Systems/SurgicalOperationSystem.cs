// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.SS220.Medicine.Surgery.Components;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.SS220.Medicine.Surgery;
using Content.Shared.SS220.Medicine.Surgery.Prototypes;
using Content.Shared.SS220.Medicine.Surgery.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Medicine.Surgery.Systems;
public sealed partial class SurgicalOperationSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SurgicalInstrumentSystem _surgicalInstrumentSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private delegate bool OperationAction(EntityUid limb, EntityUid user, SurgicalInstrumentComponent component);

    private Dictionary<SurgicalInstrumentSpecializationTypePrototype, OperationAction> SurgicalProcedures = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SurgicalInstrumentComponent, AfterInteractEvent>(OnAfterInteractEvent);
        SubscribeLocalEvent<SurgicalInstrumentComponent, SurgeryInstrumentDoAfterEvent>(OnDoAfter);

        SurgicalProcedures = new Dictionary<SurgicalInstrumentSpecializationTypePrototype, OperationAction>()
        {
            { _prototypeManager.Index<SurgicalInstrumentSpecializationTypePrototype>("Incision"), _surgicalInstrumentSystem.TryMakeIncision },
            { _prototypeManager.Index<SurgicalInstrumentSpecializationTypePrototype>("Clamp"), _surgicalInstrumentSystem.TryMakeClamp },
            { _prototypeManager.Index<SurgicalInstrumentSpecializationTypePrototype>("Retract"), _surgicalInstrumentSystem.TryMakeRetract },
            { _prototypeManager.Index<SurgicalInstrumentSpecializationTypePrototype>("Cauter"), _surgicalInstrumentSystem.TryMakeCauter },
            { _prototypeManager.Index<SurgicalInstrumentSpecializationTypePrototype>("Drill"), _surgicalInstrumentSystem.TryMakeDrill },
            { _prototypeManager.Index<SurgicalInstrumentSpecializationTypePrototype>("Debridement"), _surgicalInstrumentSystem.TryMakeDebridement },
            { _prototypeManager.Index<SurgicalInstrumentSpecializationTypePrototype>("Saw"), _surgicalInstrumentSystem.TryMakeSaw },
            { _prototypeManager.Index<SurgicalInstrumentSpecializationTypePrototype>("Amputation"), _surgicalInstrumentSystem.TryMakeAmputation },
        };
    }

    public void OnAfterInteractEvent(EntityUid uid, SurgicalInstrumentComponent component, AfterInteractEvent args)
    {
        if (!args.CanReach || args.Target is not { } target)
            return;
        if (!TryComp<SurgeonComponent>(target, out var surgeonComponent))
            return;

        switch (component.Mode)
        {
            case SurgicalInstrumentMode.SELECTOR:
                _surgicalInstrumentSystem.PopulateLimbSelector(args.User, target, args.Used);
                break;
            case SurgicalInstrumentMode.OPERATION:
                TryMakeOperationalStep(uid, component, args);
                break;
        }
    }

    public void TryMakeOperationalStep(EntityUid uid, SurgicalInstrumentComponent component, AfterInteractEvent args)
    {
        if (component.Target == null || component.Target is null)
            return;

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, component.UsageTime, new SurgeryInstrumentDoAfterEvent(), uid, target: args.Target, used: uid)
        {
            BreakOnTargetMove = true,
            BreakOnUserMove = true,
            NeedHand = true
        });
    }

    /// <summary>
    /// Entry point in other steps (dependencies on tool qualities)
    /// </summary>
    public void OnDoAfter(EntityUid uid, SurgicalInstrumentComponent component, SurgeryInstrumentDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        if (args.Target is null || args.Used is null)
            return;

        foreach (var specialization in component.Specialization)
        {
            if (!_prototypeManager.TryIndex<SurgicalInstrumentSpecializationTypePrototype>(specialization, out var prototype))
                continue;

            if (!SurgicalProcedures.ContainsKey(prototype))
                continue;

            try
            {
                SurgicalProcedures[prototype].Invoke(args.Target.Value, args.Used.Value, component);
            }
            catch (Exception e)
            {
                Log.Error($"Can't execute given SurgicalProcedure! Aborting execution! {e.Message}");
                break;
            }
        }
    }

}