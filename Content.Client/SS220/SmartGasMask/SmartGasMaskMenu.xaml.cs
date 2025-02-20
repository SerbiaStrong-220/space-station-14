// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Numerics;
using Content.Client.UserInterface.Controls;
using Content.Shared.SS220.SmartGasMask;
using Content.Shared.SS220.SmartGasMask.Prototype;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Prototypes;

namespace Content.Client.SS220.SmartGasMask;

public sealed partial class SmartGasMaskMenu : RadialMenu
{
    public event Action<ProtoId<AlertSmartGasMaskPrototype>>? SendAlertSmartGasMaskRadioMessageAction;

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

        if (!_entityManager.TryGetComponent<SmartGasMaskComponent>(Entity, out var comp))
            return;

        foreach (var smartProtoString in comp.SelectablePrototypes)
        {
            if (!_prototypeManager.TryIndex<AlertSmartGasMaskPrototype>(smartProtoString, out var alertProto))
                continue;

            var button = new SmartGasMaskMenuButton()
            {
                StyleClasses = { "RadialMenuButton" },
                SetSize = new Vector2(64, 64),
                ToolTip = Loc.GetString(alertProto.Name),
                ProtoId = alertProto.ID,
            };

            var entProtoView = new EntityPrototypeView()
            {
                SetSize = new Vector2(48, 48),
                VerticalAlignment = VAlignment.Center,
                HorizontalAlignment = HAlignment.Center,
                Stretch = SpriteView.StretchMode.Fill
            };

            if (_prototypeManager.TryIndex(alertProto.IconPrototype, out var iconProto))
                entProtoView.SetPrototype(iconProto);
            else
                entProtoView.SetPrototype(alertProto.EntityPrototype);

            button.AddChild(entProtoView);
            main.AddChild(button);
            AddSmartGasMaskMenuButtoOnClickActions(main);
        }
    }

    private void AddSmartGasMaskMenuButtoOnClickActions(Control control)
        {
            var mainControl = control as RadialContainer;

            if (mainControl == null)
                return;

            foreach (var child in mainControl.Children)
            {
                var castChild = child as SmartGasMaskMenuButton;

                if (castChild == null)
                    continue;

                castChild.OnButtonUp += _ =>
                {
                    SendAlertSmartGasMaskRadioMessageAction?.Invoke(castChild.ProtoId);
                    Close();
                };
            }
        }
}

public sealed class SmartGasMaskMenuButton : RadialMenuTextureButton
{
    public ProtoId<AlertSmartGasMaskPrototype> ProtoId { get; set; }
}

