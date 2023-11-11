
using Content.Server.Popups;
using Content.Server.SS220.Surgery.Components;
using Content.Shared.Body.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.SS220.Surgery;
using Content.Shared.SS220.Surgery.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Player;

namespace Content.Server.SS220.Surgery.Systems
{
    public sealed partial class SurgicalInstrumentSystem : EntitySystem
    {
        [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
        [Dependency] private readonly PopupSystem _popup = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SurgicalInstrumentComponent, UseInHandEvent>(SwitchInstrumentMode);
            SubscribeLocalEvent<SurgicalInstrumentComponent, SelectorButtonPressed>(UpdateTarget);
        }

        public void SwitchInstrumentMode(EntityUid uid, SurgicalInstrumentComponent component, UseInHandEvent args)
        {
            var instrumentPopup = component.Mode == SurgicalInstrumentMode.SELECTOR ? "Оперирование" : "Выбор зоны операции";
            component.Mode = component.Mode == SurgicalInstrumentMode.SELECTOR ? SurgicalInstrumentMode.OPERATION : SurgicalInstrumentMode.SELECTOR;
            _popup.PopupEntity(instrumentPopup, args.User);
        }

        public void PopulateLimbSelector(EntityUid user, EntityUid target, EntityUid instrument)
        {
            if (!TryComp<ActorComponent>(user, out var actor))
                return;

            var bui = _uiSystem.GetUiOrNull(instrument, LimbSelectorUiKey.Key);
            if (bui != null)
            {
                _uiSystem.OpenUi(bui, actor.PlayerSession);
                _uiSystem.SendUiMessage(bui, new InstrumentUsedAfterInteractEvent(GetNetEntity(target)));
            }
        }

        public void UpdateTarget(EntityUid uid, SurgicalInstrumentComponent component, SelectorButtonPressed msg)
        {
            component.Target = GetEntity(msg.LimbId);
        }

        // Tool methods
        public bool TryMakeIncision(EntityUid limb, EntityUid user, SurgicalInstrumentComponent component)
        {
            _popup.PopupEntity($"ЫААААААААААААААААААААААААА {Name(limb)} фулл разнос", user);
            return true;
        }

        public bool TryMakeClamp(EntityUid limb, EntityUid user, SurgicalInstrumentComponent component)
        {
            _popup.PopupEntity($"ЫААААААААААААААААААААААААА {Name(limb)} фулл перекрут", user);
            return true;
        }

        public bool TryMakeRetract(EntityUid limb, EntityUid user, SurgicalInstrumentComponent component)
        {
            return true;
        }
    }
}
