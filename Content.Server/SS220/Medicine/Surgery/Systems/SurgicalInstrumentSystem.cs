// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Popups;
using Content.Server.SS220.Medicine.Surgery.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction.Events;
using Content.Shared.SS220.Medicine.Injury.Components;
using Content.Shared.SS220.Medicine.Injury.Systems;
using Content.Shared.SS220.Medicine.Surgery;
using Content.Shared.SS220.Medicine.Surgery.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Medicine.Surgery.Systems;
public sealed partial class SurgicalInstrumentSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedInjurySystem _injureSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedBodySystem _sharedBody = default!;

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
        component.Target = GetEntity(msg.TargetId);
    }

    // Tool methods
    public bool TryMakeIncision(EntityUid target, EntityUid user, SurgicalInstrumentComponent component)
    {
        if (component.Target == null || !TryComp<InjuriesContainerComponent>(component.Target, out var injured))
            return false;

        var incisedWound = _injureSystem.AddInjure(component.Target!.Value, injured, InjurySeverityStages.LIGHT, "IncisedWound");
        var bloodLoss = _injureSystem.AddInjure(component.Target!.Value, injured, InjurySeverityStages.LIGHT, "InternalBleeding");

        _popup.PopupEntity($"{Name(component.Target!.Value)} была прооперирована, оставив {Name(incisedWound)}!", user);
        return true;
    }
    public bool TryMakeClamp(EntityUid limb, EntityUid user, SurgicalInstrumentComponent component)
    {
        if (component.Target == null || !TryComp<InjuriesContainerComponent>(component.Target, out var injured))
            return false;
        return true;
    }

    public bool TryMakeRetract(EntityUid limb, EntityUid user, SurgicalInstrumentComponent component)
    {
        return true;
    }

    public bool TryMakeCauter(EntityUid limb, EntityUid user, SurgicalInstrumentComponent component)
    {
        return true;
    }

    public bool TryMakeDrill(EntityUid limb, EntityUid user, SurgicalInstrumentComponent component)
    {
        return true;
    }

    public bool TryMakeDebridement(EntityUid limb, EntityUid user, SurgicalInstrumentComponent component)
    {
        return true;
    }

    public bool TryMakeSaw(EntityUid limb, EntityUid user, SurgicalInstrumentComponent component)
    {
        return true;
    }

    public bool TryMakeAmputation(EntityUid limb, EntityUid user, SurgicalInstrumentComponent component)
    {
        return true;
    }
}
