
using JetBrains.Annotations;

namespace Content.Client.SS220.Medicine.InjureSelector;

[UsedImplicitly]
public sealed partial class InjureSelectorBoundUserInterface : BoundUserInterface
{
    public InjureSelectorLayout? _layout;
    public InjureSelectorBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {

    }

    protected override void Open()
    {
        base.Open();
        _layout = new InjureSelectorLayout();
        _layout.OpenCentered();
        _layout.OnClose += Close;
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        base.ReceiveMessage(message);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
    }
}