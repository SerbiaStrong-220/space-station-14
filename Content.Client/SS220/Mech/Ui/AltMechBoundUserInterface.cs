using Content.Client.Mech;
using Content.Client.UserInterface.Fragments;
using Content.Shared.Mech;
using Content.Shared.SS220.AltMech;
using Content.Shared.SS220.Mech.Components;
using Content.Shared.SS220.Mech.Equipment.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Robust.Client.UserInterface.Controls.MenuBar;

namespace Content.Client.SS220.Mech.Ui;

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
        base.Open();

        _menu = this.CreateWindowCenteredLeft<AltMechMenu>();


        _menu.SetEntity(Owner, MechPartVisualLayers.Core);

        foreach (var part in mechComp.ContainerDict)
        {
            _menu.SetEntity(part.Value.ContainedEntity, partsVisuals[part.Key]);
        }

        _menu.OnRemovePartButtonPressed += part => SendMessage(new MechPartRemoveMessage(part));
        _menu.OnRemovePartButtonPressed += part => UpdateStateAfterButtonPressed(part);

        _menu.OnMaintenancePressed += toggled => SendMessage(new MechMaintenanceToggleMessage(toggled));

        _menu?.UpdateMechStats();
        _menu?.UpdateEquipmentView();

        _menu?.SetMaintenance(mechComp.MaintenanceMode);
    }

    protected void UpdateStateAfterButtonPressed(string _)
    {
        //_menu?.UpdateMechStats();
        //_menu?.UpdateEquipmentView();
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

            foreach (var ent in partComp.EquipmentContainer.ContainedEntities)
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

