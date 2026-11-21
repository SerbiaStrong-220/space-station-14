// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.CustomControls;
using static Robust.Client.UserInterface.Controls.BoxContainer;
using System.Numerics;

namespace Content.Client.SS220.AntagRemoval.Notifications;

public sealed class RoleRemovalNoticeWindow : DefaultWindow
{
    public readonly Button AcceptButton;
    private readonly Label _message;

    public RoleRemovalNoticeWindow()
    {
        Title = Loc.GetString("ss220-role-removal-notice-title");

        Contents.AddChild(new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            Children =
            {
                new BoxContainer
                {
                    Orientation = LayoutOrientation.Vertical,
                    Children =
                    {
                        (_message = new Label
                        {
                            Text = Loc.GetString("ss220-role-removal-notice-text")
                        }),
                        new BoxContainer
                        {
                            Orientation = LayoutOrientation.Horizontal,
                            Align = AlignMode.Center,
                            Children =
                            {
                                (AcceptButton = new Button
                                {
                                    Text = Loc.GetString("ss220-role-removal-notice-accept-button"),
                                }),

                                (new Control
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

    public void SetRemovedRole(string? role)
    {
        _message.Text = role is null
            ? Loc.GetString("ss220-role-removal-notice-text")
            : Loc.GetString("ss220-role-removal-notice-antag-text", ("role", role));
    }
}
