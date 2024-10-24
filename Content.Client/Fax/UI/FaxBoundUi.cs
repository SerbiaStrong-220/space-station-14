using System.IO;
using System.Threading.Tasks;
using Content.Shared.Fax;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client.Fax.UI;

[UsedImplicitly]
public sealed class FaxBoundUi : BoundUserInterface
{
    [Dependency] private readonly IFileDialogManager _fileDialogManager = default!;

    [ViewVariables]
    private FaxWindow? _window;

    private bool _dialogIsOpen = false;

    public FaxBoundUi(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<FaxWindow>();
        // SS220 Printer (And fax overhaul)
        // _window.FileButtonPressed += OnFileButtonPressed;
        // _window.CopyButtonPressed += OnCopyButtonPressed;
        _window.SendButtonPressed += OnSendButtonPressed;
        _window.RefreshButtonPressed += OnRefreshButtonPressed;
        _window.PeerSelected += OnPeerSelected;
    }

    private void OnSendButtonPressed()
    {
        SendMessage(new FaxSendMessage());
    }

    private void OnRefreshButtonPressed()
    {
        SendMessage(new FaxRefreshMessage());
    }

    private void OnPeerSelected(string address)
    {
        SendMessage(new FaxDestinationMessage(address));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_window == null || state is not FaxUiState cast)
            return;

        _window.UpdateState(cast);
    }
}
