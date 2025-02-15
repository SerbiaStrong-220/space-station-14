using Robust.Client.UserInterface;

namespace Content.Client.SS220.SmartGasMask;

public sealed class GhostRoleRadioBoundUserInterface : BoundUserInterface
{
    private SmartGasMaskMenu? _smartGasMaskMenu;

    public GhostRoleRadioBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();

        _smartGasMaskMenu = this.CreateWindow<SmartGasMaskMenu>();
        _smartGasMaskMenu.SetEntity(Owner);
        _smartGasMaskMenu.SendGhostRoleRadioMessageAction += SendGhostRoleRadioMessage;
    }
}
