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

        private void OnExamined(Entity<AutoinjectorComponent> entity, ref ExaminedEvent ev)
        {
            if (entity.Comp.Used)
                ev.PushMarkup(Loc.GetString(entity.Comp.OnExaminedMessage));
        }

        private void OnAfterHypo(Entity<AutoinjectorComponent> entity, ref AfterHypoEvent ev)
        {
            if (!TryComp<HyposprayComponent>(entity, out var hypoComp)
            || !_solutionContainerSystem.TryGetSolution(entity.Owner, hypoComp.SolutionName, out _, out _))
                return;

            RemComp<RefillableSolutionComponent>(entity);
            entity.Comp.Used = true;

            var message = Loc.GetString(entity.Comp.OnUseMessage);
            _popup.PopupEntity(message, ev.Target, ev.User);
        }
    }
}

