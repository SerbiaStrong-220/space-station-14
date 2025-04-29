// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Client.UserInterface;

namespace Content.Client.SS220.Surgery.BodyAnalyzerUi;

public sealed class BodyAnalyzerBUI(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private BodyAnalyzerMenu? _menu;

    protected override void Open()
    {
        base.Open();
        _menu = this.CreateWindow<BodyAnalyzerMenu>();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
    }

}
