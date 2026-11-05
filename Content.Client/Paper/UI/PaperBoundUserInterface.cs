using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;
using Content.Shared.Paper;
using static Content.Shared.Paper.PaperComponent;
using Content.Shared.SS220.Paper;
using Content.Shared.SS220.Signature;

namespace Content.Client.Paper.UI;

[UsedImplicitly]
public sealed class PaperBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private PaperWindow? _window;

    public PaperBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<PaperWindow>();
        _window.OnSaved += InputOnTextEntered;

        // SS220 Document helper begin
        _window.DocumentHelper.OnButtonPressed += args => _window.InsertAtCursor(args);
        // SS220 Document helper end

        if (EntMan.TryGetComponent<PaperComponent>(Owner, out var paper))
        {
            _window.MaxInputLength = paper.ContentSize;
        }
        if (EntMan.TryGetComponent<PaperVisualsComponent>(Owner, out var visuals))
        {
            _window.InitVisuals(Owner, visuals);
        }

        // ss220 add signature start
        if (EntMan.TryGetComponent<SignatureComponent>(Owner, out var signature))
        {
            _window.SignatureContainer.Visible = true;

            if (signature.Data != null)
             _window.InitSignature(signature.Data);
        }

        _window.LoadSavedSignatureData += OnLoadSavedSignatureData;
        // ss220 add signature end
    }

    // ss220 add signature start
    private void OnLoadSavedSignatureData()
    {
        var ev = new ApplySavedSignature();
        SendPredictedMessage(ev);
    }
    // ss220 add signature end

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        // ss220 add signature start
        if (state is PaperBoundUserInterfaceState paperState)
            _window?.Populate(paperState);

        if (_window == null)
            return;

        if (state is UpdatePenBrushPaperState brushState)
            _window.UpdateBrush(brushState.BrushWriteSize, brushState.BrushEraseSize);

        if (state is UpdateSignatureDataState signatureState)
            _window.Signature.SetSignature(signatureState.Data);
        // ss220 add signature end
    }

    // SS220 Document Helper begin
    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        base.ReceiveMessage(message);

        if (message is DocumentHelperOptionsMessage optionsMessage)
            _window?.DocumentHelper.UpdateState(optionsMessage);
    }
    // SS220 Document Helper end

    private void InputOnTextEntered(string text)
    {
        SendMessage(new PaperInputTextMessage(text));

        if (_window != null)
        {
            _window.Input.TextRope = Rope.Leaf.Empty;
            _window.Input.CursorPosition = new TextEdit.CursorPos(0, TextEdit.LineBreakBias.Top);

            // ss220 add signature start
            if (_window.Signature.Data != null)
                SendMessage(new SignatureSubmitMessage(_window.Signature.Data));
            // ss220 add signature end
        }
    }
}
