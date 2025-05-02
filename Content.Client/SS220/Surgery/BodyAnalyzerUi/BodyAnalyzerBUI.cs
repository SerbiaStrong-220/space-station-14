// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Surgery.Components;
using Content.Shared.SS220.Surgery.Ui;
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

        _menu.UpdatePerformer();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        switch (state)
        {
            case BodyAnalyzerTargetUpdate msg:
                var target = EntMan.GetEntity(msg.Target);
                _menu?.ChangeTarget(target, EntMan.GetComponentOrNull<OnSurgeryComponent>(target)?.SurgeryGraphProtoId);
                _menu?.UpdatePerformer();
                break;
        }
    }

}
