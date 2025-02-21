// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Input;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;

namespace Content.Client.SS220.Guidebook.Richtext;

[UsedImplicitly]
public sealed class BrowserLinkTag : IMarkupTag
{
    [Dependency] private readonly IUriOpener _uriOpener = default!;
    [Dependency] private readonly ILogManager _logMan = default!;

    public string Name => "browserlink";

    private ISawmill Log => _log ??= _logMan.GetSawmill("protodata_tag");
    private ISawmill? _log;

    public bool TryGetControl(MarkupNode node, [NotNullWhen(true)] out Control? control)
    {
        if (!node.Attributes.TryGetValue("link", out var linkParametr) ||
            !linkParametr.TryGetString(out var link))
        {
            control = null;
            return false;
        }

        node.Value.TryGetString(out var text);

        var label = new Label();
        if (text != null)
            label.Text = text;
        else
            label.Text = link;

        label.MouseFilter = Control.MouseFilterMode.Stop;
        label.FontColorOverride = Color.CornflowerBlue;
        label.DefaultCursorShape = Control.CursorShape.Hand;

        label.OnMouseEntered += _ => label.FontColorOverride = Color.LightSkyBlue;
        label.OnMouseExited += _ => label.FontColorOverride = Color.CornflowerBlue;
        label.OnKeyBindDown += args => OnKeybindDown(args, link);

        control = label;
        return true;
    }

    private void OnKeybindDown(GUIBoundKeyEventArgs args, string link)
    {
        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        try
        {
            _uriOpener.OpenUri(link);
        }
        catch (Exception e)
        {
            Log.Error(e.Message);
        }
    }
}
