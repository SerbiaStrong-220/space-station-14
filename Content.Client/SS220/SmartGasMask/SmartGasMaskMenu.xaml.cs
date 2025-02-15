using System.Numerics;
using Content.Client.UserInterface.Controls;
using Content.Shared.Ghost.Roles.Components;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Prototypes;

namespace Content.Client.SS220.SmartGasMask;

public sealed partial class SmartGasMaskMenu : RadialMenu
{
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public EntityUid Entity { get; set; }

    public SmartGasMaskMenu()
    {
        IoCManager.InjectDependencies(this);
        RobustXamlLoader.Load(this);
    }

    public void SetEntity(EntityUid uid)
    {
        Entity = uid;
        RefreshUI();
    }

    private void RefreshUI()
    {
        var main = FindControl<RadialContainer>("Main");

        if (!_entityManager.TryGetComponent<GhostRoleMobSpawnerComponent>(Entity, out var comp))
            return;

        foreach (var ghostRoleProtoString in comp.SelectablePrototypes)
        {
            if (!_prototypeManager.TryIndex<GhostRolePrototype>(ghostRoleProtoString, out var ghostRoleProto))
                continue;

            var button = new GhostRoleRadioMenuButton()
            {
                StyleClasses = { "RadialMenuButton" },
                SetSize = new Vector2(64, 64),
                ToolTip = Loc.GetString(ghostRoleProto.Name),
                ProtoId = ghostRoleProto.ID,
            };

            var entProtoView = new EntityPrototypeView()
            {
                SetSize = new Vector2(48, 48),
                VerticalAlignment = VAlignment.Center,
                HorizontalAlignment = HAlignment.Center,
                Stretch = SpriteView.StretchMode.Fill
            };

            if (_prototypeManager.TryIndex(ghostRoleProto.IconPrototype, out var iconProto))
                entProtoView.SetPrototype(iconProto);
            else
                entProtoView.SetPrototype(ghostRoleProto.EntityPrototype);

            button.AddChild(entProtoView);
            main.AddChild(button);
            AddGhostRoleRadioMenuButtonOnClickActions(main);
        }
    }

    private void AddGhostRoleRadioMenuButtonOnClickActions(Control control)
        {
            var mainControl = control as RadialContainer;

            if (mainControl == null)
                return;

            foreach (var child in mainControl.Children)
            {
                var castChild = child as GhostRoleRadioMenuButton;

                if (castChild == null)
                    continue;

                castChild.OnButtonUp += _ =>
                {
                    SendGhostRoleRadioMessageAction?.Invoke(castChild.ProtoId);
                    Close();
                };
            }
        }
}

public sealed class GhostRoleRadioMenuButton : RadialMenuTextureButton
{
    public ProtoId<GhostRolePrototype> ProtoId { get; set; }
}

