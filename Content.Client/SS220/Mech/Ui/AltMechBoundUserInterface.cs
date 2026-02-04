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
        base.Open();

        _menu = this.CreateWindowCenteredLeft<AltMechMenu>();

        if (!_ent.TryGetComponent<AltMechComponent>(Owner, out var mechComp))
            return;

        _menu.SetEntity(Owner, MechPartVisualLayers.Core);

        foreach (var part in mechComp.ContainerDict)
        {
            _menu.SetEntity(part.Value.ContainedEntity, partsVisuals[part.Key]);
        }

        _menu.OnRemovePartButtonPressed += id => SendMessage(new MechPartRemoveMessage(id));
        _menu.OnRemovePartButtonPressed += id => UpdateStateAfterButtonPressed(id);
    }

    protected void UpdateStateAfterButtonPressed(string _)
    {
        //_menu?.UpdateMechStats();
        //_menu?.UpdateEquipmentView();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not MechBoundUiState msg)
            return;

        UpdateEquipmentControls(msg);
        _menu?.UpdateMechStats();
        _menu?.UpdateEquipmentView();
    }

    public void UpdateEquipmentControls(MechBoundUiState state)
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

