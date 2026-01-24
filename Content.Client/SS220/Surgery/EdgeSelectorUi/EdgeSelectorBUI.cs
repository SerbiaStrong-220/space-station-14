// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Client.UserInterface.Controls;
using Content.Shared.SS220.Surgery.Graph;
using Content.Shared.SS220.Surgery.Ui;
using Robust.Client.UserInterface;
using Robust.Shared.Collections;
using Robust.Shared.Prototypes;

namespace Content.Client.SS220.Surgery.EdgeSelectorUi;

public sealed class EdgeSelectorBUI(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private SimpleRadialMenu? _menu;

    private readonly Color _backGroundUnavailableEdge = Color.DarkGray;
    private readonly Color _hoverBackGroundUnavailableEdge = Color.Gray;

    protected override void Open()
    {
        base.Open();
        _menu = this.CreateWindow<SimpleRadialMenu>();
        _menu.Track(Owner);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        switch (state)
        {
            case SurgeryEdgeSelectorEdgesState msg:

                var buttons = ConvertInfosToButtons(msg.Infos);
                _menu?.SetButtons(buttons);

                _menu?.OpenOverMouseScreenPosition();

                break;
        }
    }

    private IEnumerable<RadialMenuOptionBase> ConvertInfosToButtons(List<EdgeSelectInfo> selectInfos)
    {
        ValueList<RadialMenuActionOptionBase> actions = new(selectInfos.Count);
        foreach (var info in selectInfos)
        {
            var action = new RadialMenuDoubleArgumentActionOption<ProtoId<SurgeryGraphPrototype>, string>(SendSelectedEdge, info.SurgeryProtoId, info.TargetNode)
            {
                IconSpecifier = RadialMenuIconSpecifier.With(info.Icon),
                ToolTip = Loc.GetString(info.Tooltip),
                BackgroundColor = info.MetEdgeRequirement ? null : _backGroundUnavailableEdge,
                HoverBackgroundColor = info.MetEdgeRequirement ? null : _hoverBackGroundUnavailableEdge,
            };

            actions.Add(action);
        }

        return actions;
    }

    private void SendSelectedEdge(ProtoId<SurgeryGraphPrototype> surgeryId, string targetNode)
    {
        var msg = new SurgeryEdgeSelectorEdgeSelectedMessage()
        {
            SurgeryId = surgeryId,
            TargetNode = targetNode
        };

        SendMessage(msg);
    }

    public sealed class RadialMenuDoubleArgumentActionOption<T1, T2>(Action<T1, T2> onPressed, T1 data1, T2 data2) : RadialMenuActionOptionBase(onPressed: () => onPressed(data1, data2));
}
