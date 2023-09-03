using Content.Server.Body.Systems;
using Content.Server.Construction;
using Content.Server.Construction.Components;
using Content.Server.Popups;
using Content.Server.SS220.Surgery.Components;
using Content.Server.SS220.Surgery.Components.Instruments;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Hands.Components;
using Content.Shared.Verbs;

namespace Content.Server.SS220.Surgery.Systems
{
    public sealed class SurgicalOperationSystem : EntitySystem
    {
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly BodySystem _bodySystem = default!;
        [Dependency] private readonly ConstructionSystem _constructionSystem = default!;

        public enum OperationsList : byte
        {
            OrganManipulation,
            LimbManipulation, // Amputation, Attachment etc. -> Не используется пока оффы не родят нормальную систему повреждений
            PlasticSurgery
        }

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<BodyComponent, GetVerbsEvent<EquipmentVerb>>(AddStartOperationVerb);
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
        }

        /// <summary>
        /// Как выбрана область, в которой мы работаем и тип операции, можем войти в режим операции
        /// На компонент оперируемового накидывается тип операции над ним
        /// С днём выбегающих из операционной челиков со вспоротым брюхом/лицом roflanebalomoment 
        /// </summary>

        public void AddStartOperationVerb(EntityUid uid, BodyComponent component, GetVerbsEvent<EquipmentVerb> args)
        {
            if (args.Target == args.User || !args.CanInteract || !args.CanAccess)
                return;

            if (!TryComp<BodyComponent>(args.Target, out var body))
                return;
            if (!TryComp<HandsComponent>(args.User, out var hands))
                return;

            if (!TryComp<OperapableComponent>(args.Target, out var operapable))
                return;

            if (!TryComp<SurgicalIncisionComponent>(hands.ActiveHandEntity, out var instrument))
                return;

            EquipmentVerb operationVerb = new()
            {
                Text = !operapable.IsOperated ? "Начать операцию" : "Прекратить операцию",
                Act = () =>
                {
                    operapable.IsOperated ^= true;
                    operapable.CurrentOperation = operapable.IsOperated ? instrument.SelectedOperationMode : null;
                }
            };
            args.Verbs.Add(operationVerb);
        }

        public void StartSurgicalSteps(EntityUid uid, BodyPartType bodyPart, byte operationType)
        {

        }

    }
}
