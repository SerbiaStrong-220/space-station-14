// SS220 Changeling
using Content.Client.Stylesheets.Palette;
using Content.Client.UserInterface.Controls;
using Content.Shared.Changeling.Components;
using Content.Shared.Changeling.Systems;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Utility;

namespace Content.Client.Changeling.UI;

[UsedImplicitly]
public sealed partial class ChangelingTransformBoundUserInterface : BoundUserInterface
{
    private SimpleRadialMenu? _menu;
    private readonly bool _isTransformationSting;
    private static readonly Color SelectedOptionBackground = Palettes.Green.Element.WithAlpha(128);
    private static readonly Color DisabledOptionBackground = Palettes.Slate.Element.WithAlpha(128);
    private static readonly Color SelectedOptionHoverBackground = Palettes.Green.HoveredElement.WithAlpha(128);
    private static readonly Color DisabledOptionHoverBackground = Palettes.Slate.HoveredElement.WithAlpha(128);

    public ChangelingTransformBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _isTransformationSting = uiKey.Equals(ChangelingTransformUiKey.TransformationSting);
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<SimpleRadialMenu>();
        Update();
        _menu.OpenOverMouseScreenPosition();
    }

    public override void Update()
    {
        if (_menu == null)
            return;

        if (!EntMan.TryGetComponent<ChangelingIdentityComponent>(Owner, out var lingIdentity))
            return;

        var models = ConvertToButtons(lingIdentity.StoredIdentities, lingIdentity.CurrentIdentity);

        _menu.SetButtons(models);
    }

    private IEnumerable<RadialMenuOptionBase> ConvertToButtons(
        IEnumerable<EntityUid> identities,
        EntityUid? currentIdentity
    )
    {
        var buttons = new List<RadialMenuOptionBase>();
        var dropButtons = new List<RadialMenuOptionBase>();

        foreach (var identity in identities)
        {
            // Options for selecting identities.
            var option = new RadialMenuActionOption<NetEntity>(SendIdentitySelect, EntMan.GetNetEntity(identity))
            {
                IconSpecifier = RadialMenuIconSpecifier.With(identity),
                ToolTip = Loc.GetString("changeling-transform-bui-select-entity", ("entity", identity)),
                BackgroundColor = (currentIdentity == identity) ? SelectedOptionBackground : null, // mark as selected
                HoverBackgroundColor = (currentIdentity == identity) ? SelectedOptionHoverBackground : null
            };
            buttons.Add(option);

            // SS220 changeling transformation sting begin
            if (_isTransformationSting)
                continue;
            // SS220 changeling transformation sting end

            // Options for dropping identities.
            var dropOption = new RadialMenuActionOption<NetEntity>(SendIdentityDrop, EntMan.GetNetEntity(identity))
            {
                IconSpecifier = RadialMenuIconSpecifier.With(identity),
                ToolTip = (currentIdentity == identity)
                    ? Loc.GetString("changeling-transform-bui-drop-identity-cannot-drop")
                    : Loc.GetString("changeling-transform-bui-drop-identity-entity", ("entity", identity)),
                BackgroundColor = (currentIdentity == identity) ? DisabledOptionBackground : null, // cannot drop your current identity
                HoverBackgroundColor = (currentIdentity == identity) ? DisabledOptionHoverBackground : null
            };
            dropButtons.Add(dropOption);
        }

        // SS220 changeling transformation sting begin
        if (_isTransformationSting)
            return buttons;
        // SS220 changeling transformation sting end

        // Menu category for dropping identities.
        var dropMenuButton = new RadialMenuNestedLayerOption(dropButtons)
        {
            IconSpecifier = RadialMenuIconSpecifier.With(new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/delete.svg.192dpi.png"))),
            ToolTip = Loc.GetString("changeling-transform-bui-drop-identity-menu")
        };
        buttons.Add(dropMenuButton);

        return buttons;
    }

    private void SendIdentitySelect(NetEntity identityId)
    {
        // SS220 changeling transformation sting begin
        if (_isTransformationSting)
        {
            SendMessage(new ChangelingTransformationStingIdentitySelectMessage(identityId));
            return;
        }
        // SS220 changeling transformation sting end

        SendMessage(new ChangelingTransformIdentitySelectMessage(identityId));
    }

    private void SendIdentityDrop(NetEntity identityId)
    {
        SendMessage(new ChangelingTransformIdentityDropMessage(identityId));
    }
}
