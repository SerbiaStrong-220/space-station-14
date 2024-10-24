// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using System.Numerics;

namespace Content.Client.SS220.MindSlave.UI;

//Stolen from DeathReminder
public sealed class MindSlaveNotificationWindow : DefaultWindow
{
    public readonly Button AcceptButton;
    public readonly Label TextLabel;

    public MindSlaveNotificationWindow()
    {
        Title = Loc.GetString("mindslave-notification-window-title");

        Contents.AddChild(new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Children =
            {
                new BoxContainer
                {
                    Orientation = BoxContainer.LayoutOrientation.Vertical,
                    Children =
                    {
                        (TextLabel = new Label()
                        {
                        }),
                        new BoxContainer
                        {
                            Orientation = BoxContainer.LayoutOrientation.Horizontal,
                            Align = BoxContainer.AlignMode.Center,
                            Children =
                            {
                                (AcceptButton = new Button
                                {
                                    Text = Loc.GetString("mindslave-notification-window-accept"),
                                }),
                                (new Control()
                                {
                                    MinSize = new Vector2(20, 0)
                                }),
                            }
                        },
                    }
                },
            }
        });
    }
}
