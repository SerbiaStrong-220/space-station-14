using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;
using Content.Shared.Paper;
using static Content.Shared.Paper.PaperComponent;
using Content.Client.SS220.Paper.UI;
using Content.Shared.SS220.Paper;

namespace Content.Client.Paper.UI;

[UsedImplicitly]
public sealed class PaperBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private PaperWindow? _window;

    private DocumentHelperWindow? _documentHelper; // SS220 Document helper

    public PaperBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<PaperWindow>();
        _window.OnSaved += InputOnTextEntered;

        // SS220 Document helper begin
        _window.OnClose += () => _documentHelper?.Close();

        _window.OnDocumentHelperButtonPressed += () =>
        {
            var options = DocumentHelperOptions.All;
            if (_documentHelper != null && _documentHelper.IsOpen)
                _documentHelper.Close();

            _documentHelper = new DocumentHelperWindow(options);
            _documentHelper.OnButtonPressed += args => _window.InsertAtCursor(args);

            if (_documentHelper.CheckNeedServerInfo(options))
                SendMessage(new DocumentHelperRequestInfoBuiMessage(options));

            _documentHelper.OnClose += () => _documentHelper = null;
            _documentHelper.OpenCenteredRight();
        };
        // SS220 Document helper end

        if (EntMan.TryGetComponent<PaperComponent>(Owner, out var paper))
        {
            _window.MaxInputLength = paper.ContentSize;
        }
        if (EntMan.TryGetComponent<PaperVisualsComponent>(Owner, out var visuals))
        {
            _window.InitVisuals(Owner, visuals);
        }
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        // SS220 Document helper begin
        //_window?.Populate((PaperBoundUserInterfaceState) state);

        if (state is PaperBoundUserInterfaceState paperState)
            _window?.Populate(paperState);

        if (state is DocumentHelperBuiState docState)
            _documentHelper?.UpdateState(docState);
        // SS220 Document helper end
    }

    private void InputOnTextEntered(string text)
    {
        SendMessage(new PaperInputTextMessage(text));

        if (_window != null)
        {
            _window.Input.TextRope = Rope.Leaf.Empty;
            _window.Input.CursorPosition = new TextEdit.CursorPos(0, TextEdit.LineBreakBias.Top);
        }
    }
}
