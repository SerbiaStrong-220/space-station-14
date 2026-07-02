// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Containers.ItemSlots;
using Content.Shared.SS220.Pathology;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.SS220.Pathology;

[UsedImplicitly]
public sealed class DiseaseDiagnoserBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private DiseaseDiagnoserWindow? _window;

    public DiseaseDiagnoserBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<DiseaseDiagnoserWindow>();
        _window.ScanButton.OnPressed += _ => SendMessage(new DiseaseDiagnoserScanMessage());
        _window.EjectButton.OnPressed += _ => SendMessage(new ItemSlotButtonPressedEvent("diagnoserSlot"));
        _window.TransferButton.OnPressed += _ => SendMessage(new DiseaseDiagnoserTransferMutagenMessage());
        _window.CopyButton.OnPressed += _ => SendMessage(new DiseaseDiagnoserCopyMessage());
        _window.PrintButton.OnPressed += _ => SendMessage(new DiseaseDiagnoserPrintMessage());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is DiseaseDiagnoserBoundUserInterfaceState cast)
            _window?.UpdateState(cast);
    }
}
