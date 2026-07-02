// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Containers.ItemSlots;
using Content.Shared.SS220.Pathology;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.SS220.Pathology;

[UsedImplicitly]
public sealed class VaccinatorBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private VaccinatorWindow? _window;

    public VaccinatorBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<VaccinatorWindow>();
        _window.ScanButton.OnPressed += _ => SendMessage(new VaccinatorScanMessage());
        _window.EjectButton.OnPressed += _ => SendMessage(new ItemSlotButtonPressedEvent("vaccinatorSlot"));
        _window.TransferButton.OnPressed += _ => SendMessage(new VaccinatorTransferMessage());
        _window.CreateVaccineButton.OnPressed += _ => SendMessage(new VaccinatorCreateVaccineMessage());
        _window.PrintButton.OnPressed += _ => SendMessage(new VaccinatorPrintMessage());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is VaccinatorBoundUserInterfaceState cast)
            _window?.UpdateState(cast);
    }
}
