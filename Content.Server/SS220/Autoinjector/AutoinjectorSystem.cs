using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Server.Popups;
using Content.Shared.Examine;

namespace Content.Server.SS220.Autoinjector
{
    public sealed partial class AutoinjectorSystem : EntitySystem
    {
        [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
        [Dependency] private readonly PopupSystem _popup = default!;
        public override void Initialize()
        {
            SubscribeLocalEvent<AutoinjectorComponent, AfterHypoEvent>(OnAfterHypo);
            SubscribeLocalEvent<AutoinjectorComponent, ExaminedEvent>(OnExamined);
        }

        private void OnExamined(EntityUid uid, AutoinjectorComponent component, ExaminedEvent ev)
        {
            if (component.Used)
                ev.PushMarkup(Loc.GetString(component.OnExaminedMessage));
        }

        private void OnAfterHypo(EntityUid uid, AutoinjectorComponent component, AfterHypoEvent ev)
        {
            if (!TryComp<HyposprayComponent>(uid, out var hypoComp)
            || !_solutionContainerSystem.TryGetSolution(uid, hypoComp.SolutionName, out _, out _))
                return;

            RemComp<RefillableSolutionComponent>(uid);
            component.Used = true;

            var message = Loc.GetString(component.OnUseMessage);
            _popup.PopupEntity(message, ev.Target, ev.User);
        }
    }
}

