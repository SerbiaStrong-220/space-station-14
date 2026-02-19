// © FCB, MIT, full text: https://github.com/Free-code-base-14/space-station-14/blob/master/LICENSE.TXT
using Content.Client.UserInterface.Fragments;
using Content.Shared.FCB.AltMech;
using Content.Shared.FCB.Mech.Components;
using Content.Shared.FCB.Mech.Parts.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.FCB.Mech.Ui;

[UsedImplicitly]
public sealed class AltMechBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IEntityManager _ent = default!;

    [ViewVariables]
    private AltMechMenu? _menu;

    public readonly Dictionary<string, MechPartVisualLayers> partsVisuals = new Dictionary<string, MechPartVisualLayers>()
    {
        ["core"] = MechPartVisualLayers.Core,
        ["head"] = MechPartVisualLayers.Head,
        ["right-arm"] = MechPartVisualLayers.RightArm,
        ["left-arm"] = MechPartVisualLayers.LeftArm,
        ["chassis"] = MechPartVisualLayers.Chassis,
        ["power"] = MechPartVisualLayers.Power
    };

    public AltMechBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        if (!_ent.TryGetComponent<AltMechComponent>(Owner, out var mechComp))
            return;

        if (mechComp.Broken)
            return;

        base.Open();

        _menu = this.CreateWindowCenteredLeft<AltMechMenu>();


        _menu.SetEntity(Owner, MechPartVisualLayers.Core);

        foreach (var part in mechComp.ContainerDict)
        {
            _menu.SetEntity(part.Value.ContainedEntity, partsVisuals[part.Key]);
        }

        _menu.OnRemovePartButtonPressed += part => SendMessage(new MechPartRemoveMessage(part));
        //_menu.OnRemovePartButtonPressed += part => UpdateStateAfterButtonPressed(part);

        _menu.OnMaintenancePressed += toggled => SendMessage(new MechMaintenanceToggleMessage(toggled));

        _menu.OnBoltButtonPressed += _ => SendMessage(new MechBoltMessage(_));

        _menu.OnSealButtonPressed += _ => SendMessage(new MechSealMessage(_));

        _menu.OnDetachTankButtonPressed += _ => SendMessage(new MechDetachTankMessage(_));

        _menu?.UpdateMechStats();
        _menu?.UpdateEquipmentView();

        _menu?.SetMaintenance(mechComp.MaintenanceMode);
        _menu?.SetSeal(mechComp.Airtight);
        _menu?.SetBolt(mechComp.Bolted);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not AltMechBoundUiState msg)
            return;

        UpdateEquipmentControls(msg);
        _menu?.UpdateMechStats();
        _menu?.UpdateEquipmentView();
    }

    public void UpdateUI()
    {
        _menu?.UpdateMechStats();
        _menu?.UpdateEquipmentView();
    }

    public void UpdateEquipmentControls(AltMechBoundUiState state)
    {
        if (!EntMan.TryGetComponent<AltMechComponent>(Owner, out var mechComp))
            return;

        foreach (var part in mechComp.ContainerDict.Values)
        {
            if(part.ContainedEntity == null)
                continue;
            if (!EntMan.TryGetComponent<MechPartComponent>(part.ContainedEntity, out var partComp))
                continue;

            foreach (var ent in mechComp.EquipmentContainer.ContainedEntities)
            {
                var ui = GetEquipmentUi(ent);
                if (ui == null)
                    continue;
                foreach (var (attached, estate) in state.EquipmentStates)
                {
                    if (ent == EntMan.GetEntity(attached))
                        ui.UpdateState(estate);
                }
            }
        }
    }

    public UIFragment? GetEquipmentUi(EntityUid? uid)
    {
        var component = EntMan.GetComponentOrNull<UIFragmentComponent>(uid);
        component?.Ui?.Setup(this, uid);
        return component?.Ui;
    }
}

