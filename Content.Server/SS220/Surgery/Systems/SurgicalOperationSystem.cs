
using Content.Server.SS220.Surgery.Components;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.SS220.Surgery.Systems;
using FastAccessors;

namespace Content.Server.SS220.Surgery.Systems
{
    public sealed partial class SurgicalOperationSystem : EntitySystem
    {
        [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;
        [Dependency] private readonly SurgicalInstrumentSystem _surgicalInstrumentSystem = default!;

        private readonly Dictionary<SurgicalInstrumentsSpecialization, Action<EntityUid, EntityUid>> surgicalActions = new Dictionary<SurgicalInstrumentsSpecialization, Action<EntityUid, EntityUid>>();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SurgicalInstrumentComponent, AfterInteractEvent>(OnAfterInteractEvent);
            SubscribeLocalEvent<SurgicalInstrumentComponent, SurgeryInstrumentDoAfterEvent>(OnDoAfter);

            //surgicalActions.Add(SurgicalInstrumentsSpecialization.Incision, _surgicalInstrumentSystem.TryMakeIncision);
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

            
        }

    }
}
