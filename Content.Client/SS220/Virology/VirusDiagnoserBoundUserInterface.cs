// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Containers.ItemSlots;
using Content.Shared.SS220.Virology;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.SS220.Virology;

[UsedImplicitly]
public sealed class VirusDiagnoserBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private VirusDiagnoserWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<VirusDiagnoserWindow>();
        _window.ScanButton.OnPressed += _ => SendMessage(new VirusDiagnoserScanMessage());
        _window.EjectButton.OnPressed += _ => SendMessage(new ItemSlotButtonPressedEvent("diagnoserSlot"));
        _window.TransferButton.OnPressed += _ => SendMessage(new VirusDiagnoserTransferMutagenMessage());
        _window.CopyButton.OnPressed += _ => SendMessage(new VirusDiagnoserCopyMessage());
        _window.PrintButton.OnPressed += _ => SendMessage(new VirusDiagnoserPrintMessage());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is VirusDiagnoserBoundUserInterfaceState cast)
            _window?.UpdateState(cast);
    }
}
