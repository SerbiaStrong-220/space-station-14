
using Robust.Client.UserInterface;

namespace Content.Client.SS220.Attachables;

public sealed class AttachablesContainerBoundUserInterface : BoundUserInterface
{
    private AttachablesContainerMenu? _menu;

    public AttachablesContainerBoundUserInterface(EntityUid owner, Enum uiKey) : base (owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<AttachablesContainerMenu>();
        _menu.SetEntity(Owner);
    }
}
