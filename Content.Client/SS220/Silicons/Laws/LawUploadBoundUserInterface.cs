// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Containers.ItemSlots;
using Content.Shared.SS220.Silicons.Laws;
using Robust.Client.UserInterface;

namespace Content.Client.SS220.Silicons.Laws;

public sealed class LawUploadBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private LawUploadWindow? _window;

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<LawUploadWindow>();
        _window.OnApply += (target, revision) => SendMessage(new ApplyStationLawsMessage(target, revision));
        _window.OnEject += slot => SendMessage(new ItemSlotButtonPressedEvent(slot, tryInsert: false));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is LawUploadState upload)
            _window?.UpdateState(upload);
    }
}
