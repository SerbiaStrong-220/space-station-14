// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;

namespace Content.Client.SS220.AntagRemoval.UI;

public sealed class RemoveAntagRoleButton : Button
{
    public string Kind { get; }

    public RemoveAntagRoleButton(string kind, string roleName)
    {
        RobustXamlLoader.Load(this);
        Kind = kind;
        Text = Loc.GetString("ss220-removeantag-ui-role", ("role", roleName));
    }
}
