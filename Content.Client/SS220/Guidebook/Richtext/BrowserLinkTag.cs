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
        var label = new Label();
        node.Value.TryGetString(out var text);
        label.Text = text;

        if (!node.Attributes.TryGetValue("link", out var linkParametr) ||
            !linkParametr.TryGetString(out var link))
        {
            control = label;
            return true;
        }

        if (string.IsNullOrEmpty(label.Text))
            label.Text = link;

        try
        {
            var uri = new Uri(link);
            if (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
            {
                label.MouseFilter = Control.MouseFilterMode.Stop;
                label.FontColorOverride = Color.CornflowerBlue;
                label.DefaultCursorShape = Control.CursorShape.Hand;

                label.OnMouseEntered += _ => label.FontColorOverride = Color.LightSkyBlue;
                label.OnMouseExited += _ => label.FontColorOverride = Color.CornflowerBlue;
                label.OnKeyBindDown += args => OnKeybindDown(args, uri);
            }
        }
        catch (Exception e)
        {
            Log.Error(e.Message);
        }

        control = label;
        return true;
    }

    private void OnKeybindDown(GUIBoundKeyEventArgs args, Uri uri)
    {
        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        try
        {
            _uriOpener.OpenUri(uri);
        }
        catch (Exception e)
        {
            Log.Error(e.Message);
        }
    }
}
