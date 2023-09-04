using Content.Server.Body.Systems;
using Content.Server.Buckle.Systems;
using Content.Server.DoAfter;
using Content.Server.Popups;
using Content.Server.SS220.Surgery.Components;
using Content.Server.SS220.Surgery.Components.Instruments;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.DoAfter;
using Content.Shared.Hands.Components;
using Content.Shared.SS220.Surgery;
using Content.Shared.Verbs;

namespace Content.Server.SS220.Surgery.Systems
{
    public sealed class SurgicalOperationSystem : EntitySystem
    {
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly BodySystem _bodySystem = default!;
        [Dependency] private readonly DoAfterSystem _doAfter = default!;
        [Dependency] private readonly BuckleSystem _buckleSystem = default!;

        public enum OperationsList : byte
        {
            OrganManipulation,
            LimbManipulation, // Amputation, Attachment etc. -> Не используется пока оффы не родят нормальную систему повреждений
            PlasticSurgery,
            ImplantManipulation
        }

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<BodyComponent, GetVerbsEvent<EquipmentVerb>>(SurgeryManipulationVerb);
            SubscribeLocalEvent<BodyComponent, GetVerbsEvent<Verb>>(AddBodyPartManipulationVerb);
            SubscribeLocalEvent<SurgicalIncisionComponent, GetVerbsEvent<EquipmentVerb>>(AddOperationsListVerb);

            SubscribeLocalEvent<BodyComponent, PullOutOrganDoAfterEvent>(OnPullOutDoAfter);
            SubscribeLocalEvent<HandsComponent, PullInOrganDoAfterEvent>(OnPullInDoAfter);
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
                case (byte) OperationsList.OrganManipulation:
                    ToggleOrganManipulationMode(args.Target, operapable, args);
                    break;
                case (byte) OperationsList.PlasticSurgery:
                    TogglePlasticSurgeryMode(args.Target, operapable, args);
                    break;
            };
        }

        public void TogglePlasticSurgeryMode(EntityUid target, OperapableComponent comp, GetVerbsEvent<Verb> args)
        {

        }

        public void ToggleOrganManipulationMode(EntityUid target, OperapableComponent comp, GetVerbsEvent<Verb> args)
        {
            if (!TryComp<BodyComponent>(target, out var bodyComp))
                return;

            Verb head = new()
            {
                Text = "Голова",
                Act = () =>
                {
                    comp.CurrentOperatedBodyPart = BodyPartType.Head;
                },
                Category = VerbCategory.BodyPartList
            };

            args.Verbs.Add(head);

            Verb body = new()
            {
                Text = "Туловище",
                Act = () =>
                {
                    comp.CurrentOperatedBodyPart = BodyPartType.Torso;
                },
                Category = VerbCategory.BodyPartList
            };
            args.Verbs.Add(body);

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
                    component.SelectedOperationMode = (byte) OperationsList.OrganManipulation;
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
                    component.SelectedOperationMode = (byte) OperationsList.PlasticSurgery;
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
                    component.SelectedOperationMode = (byte) OperationsList.ImplantManipulation;
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
            if (args.Target == args.User || !args.CanInteract || !args.CanAccess)
                return;

            if (!TryComp<BodyComponent>(args.Target, out var body))
                return;
            if (!TryComp<HandsComponent>(args.User, out var hands))
                return;

            if (!TryComp<OperapableComponent>(args.Target, out var operapable))
                return;

            if (TryComp<SurgicalIncisionComponent>(hands.ActiveHandEntity, out var scalpel))
            {
                EquipmentVerb operationVerb = new()
                {
                    Text = !operapable.IsOperated ? "Начать операцию" : "Прекратить операцию",
                    Act = () =>
                    {
                        operapable.IsOperated ^= true;
                        operapable.CurrentOperation = operapable.IsOperated ? scalpel.SelectedOperationMode : null;
                        _popupSystem.PopupEntity("Вы приступили к оперированию", args.User);
                    }
                };
                args.Verbs.Add(operationVerb);
            };

            if (TryComp<SurgicalClampComponent>(hands.ActiveHandEntity, out var clamp) && operapable.IsOpened)
            {
                var organs = _bodySystem.GetBodyOrgans(args.Target, component);
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
            }
        }
        public void OnPullOutDoAfter(EntityUid uid, BodyComponent component, DoAfterEvent args)
        {
            if (args.Handled || args.Cancelled)
                return;
            if(!TryComp<SurgicalClampComponent>(args.Used, out var clamp))
                return;

            _bodySystem.DropOrgan(clamp.SelectedOrgan);
        }

        public void OnPullInDoAfter(EntityUid uid, HandsComponent component, DoAfterEvent args)
        {
            if (args.Handled || args.Cancelled)
                return;
        }
    };
}
