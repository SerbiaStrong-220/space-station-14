using Content.Server.Body.Systems;
using Content.Server.Buckle.Systems;
using Content.Server.DoAfter;
using Content.Server.Popups;
using Content.Server.SS220.Surgery.Components;
using Content.Server.SS220.Surgery.Components.Instruments;
using Content.Shared.Body.Components;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Part;
using Content.Shared.DoAfter;
using Content.Shared.Hands.Components;
using Content.Shared.SS220.Surgery;
using Content.Shared.SS220.Surgery.Systems;
using Content.Shared.Verbs;

/*
 * Помогите этому коду, ему хуёво
 */

namespace Content.Server.SS220.Surgery.Systems
{
    public sealed class SurgicalOperationSystem : EntitySystem
    {
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly BodySystem _bodySystem = default!;
        [Dependency] private readonly DoAfterSystem _doAfter = default!;
        [Dependency] private readonly BuckleSystem _buckleSystem = default!;
        [Dependency] private readonly IEntityManager _entitySystem = default!;

        [Dependency] private readonly SurgicalOrganManipulationSystem _organManipulation = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<BodyComponent, GetVerbsEvent<EquipmentVerb>>(SurgeryManipulationVerb);
            SubscribeLocalEvent<BodyComponent, GetVerbsEvent<Verb>>(AddBodyPartManipulationVerb);
            SubscribeLocalEvent<SurgicalIncisionComponent, GetVerbsEvent<EquipmentVerb>>(AddOperationsListVerb);

        }

        /// <summary>
        /// Выбираем с каким участком тела работаем
        /// </summary>

        public void AddBodyPartManipulationVerb(EntityUid uid, BodyComponent component, GetVerbsEvent<Verb> args)
        {
            if (!TryComp<HandsComponent>(args.User, out var hands))
                return;

            if (!TryComp<SurgicalIncisionComponent>(hands.ActiveHandEntity, out var instrument))
                return;
            if (!TryComp<OperapableComponent>(args.Target, out var operapable))
                return;

            if (!TryComp<BodyComponent>(args.Target, out var body))
                return;
            if (operapable.IsOperated)
                return;

            switch (instrument.SelectedOperationMode)
            {
                case (byte) SharedSurgeyOperationSystem.OperationsList.OrganManipulation:
                    _organManipulation.ToggleOrganManipulationMode(args.Target, operapable, args);
                    break;
                case (byte) SharedSurgeyOperationSystem.OperationsList.PlasticSurgery:
                    TogglePlasticSurgeryMode(args.Target, operapable, args);
                    break;
            };
        }

        public void TogglePlasticSurgeryMode(EntityUid target, OperapableComponent comp, GetVerbsEvent<Verb> args)
        {

        }

        /// <summary>
        /// Выбор типа операции
        /// </summary>
        public void AddOperationsListVerb(EntityUid uid, SurgicalIncisionComponent component, GetVerbsEvent<EquipmentVerb> args)
        {
            if (!TryComp<HandsComponent>(args.User, out var hands))
                return;

            EquipmentVerb limbManipulationVerb = new()
            {
                Text = "Манипуляция с органами",
                Act = () =>
                {
                    component.SelectedOperationMode = (byte) SharedSurgeyOperationSystem.OperationsList.OrganManipulation;
                    _popupSystem.PopupEntity("Выбрана операция 'Манипуляция с органами'", uid);
                },
                Category = VerbCategory.SurgeyOperations
            };
            args.Verbs.Add(limbManipulationVerb);

            EquipmentVerb plasticSurgeryVerb = new()
            {
                Text = "Пластическая хирургия",
                Act = () =>
                {
                    component.SelectedOperationMode = (byte) SharedSurgeyOperationSystem.OperationsList.PlasticSurgery;
                    _popupSystem.PopupEntity("Выбрана операция 'Пластическая хирургия'", uid);
                },
                Category = VerbCategory.SurgeyOperations
            };
            args.Verbs.Add(plasticSurgeryVerb);

            EquipmentVerb implantManipulationVerb = new()
            {
                Text = "Манипуляция с имплантами",
                Act = () =>
                {
                    component.SelectedOperationMode = (byte) SharedSurgeyOperationSystem.OperationsList.ImplantatManipulation;
                    _popupSystem.PopupEntity("Выбрана операция 'Манипуляция с имплантами'", uid);
                },
                Category = VerbCategory.SurgeyOperations
            };
            args.Verbs.Add(implantManipulationVerb);
        }

        /// <summary>
        /// Как выбрана область, в которой мы работаем и тип операции, можем войти в режим операции
        /// На компонент оперируемового накидывается тип операции над ним
        /// С днём выбегающих из операционной челиков со вспоротым брюхом/лицом roflanebalomoment 
        /// </summary>

        public void SurgeryManipulationVerb(EntityUid uid, BodyComponent component, GetVerbsEvent<EquipmentVerb> args)
        {
            if (args.Target == args.User || !args.CanInteract || !args.CanAccess || !_buckleSystem.IsBuckled(args.Target))
                return;
            if (!TryComp<BodyComponent>(args.Target, out var body))
                return;
            if (!TryComp<HandsComponent>(args.User, out var hands))
                return;

            if (!TryComp<OperapableComponent>(args.Target, out var operapable))
                return;

            if (TryComp<SurgicalIncisionComponent>(hands.ActiveHandEntity, out var scalpel)) // -> Вынести в TryStartOperation
            {
                EquipmentVerb operationVerb = new()
                {
                    Text = !operapable.IsOperated ? "Начать операцию" : "Прекратить операцию",
                    Act = () =>
                    {
                        operapable.IsOperated ^= true;
                        operapable.CurrentOperation = operapable.IsOperated ? scalpel.SelectedOperationMode : null;
                        _popupSystem.PopupEntity(operapable.IsOperated ? "Вы приступили к оперированию" : "Вы прекратили операцию", args.User);
                    }
                };
                args.Verbs.Add(operationVerb);
            };
            if (TryComp<SurgicalClampComponent>(hands.ActiveHandEntity, out var clamp) && operapable.IsOpened) // -> Вынести в OrganManipulation.TryPullOutOrgan
            {
                var organs = _bodySystem.GetBodyOrgans(args.Target);
                foreach (var organ in organs)
                {
                    EquipmentVerb verb = new()
                    {
                        Text = Name(organ.Id),
                        Act = () =>
                        {
                            clamp.SelectedOrgan = organ.Id;
                            var doAfter = new DoAfterArgs(args.User, 3, new PullOutOrganDoAfterEvent(), args.Target, target: args.Target, used: hands.ActiveHandEntity)
                            {
                                BreakOnTargetMove = true,
                                BreakOnUserMove = true,
                                BreakOnDamage = true,
                                NeedHand = true
                            };

                            _doAfter.TryStartDoAfter(doAfter);
                        },
                        Category = VerbCategory.OrganList
                    };
                    args.Verbs.Add(verb);
                };
            };
        }

        public bool TryStartOperation()
        {
            return false;
        }

    };
}
