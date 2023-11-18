// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using Content.Client.SS220.Medicine.InjureSelector;
using Content.Shared.Body.Systems;
using Content.Shared.SS220.Medicine.Surgery;
using JetBrains.Annotations;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;
using Serilog;

namespace Content.Client.SS220.Medicine.Selector;

[UsedImplicitly]
public sealed partial class LimbSelectorBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IEntitySystemManager _sysMan = default!;
    [Dependency] private readonly EntityManager _entMan = default!;
    public LimbSelectorLayout? _layout;
    public InjureSelectorLayout? _injureLayout;
    public LimbSelectorBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _layout = new LimbSelectorLayout();
        _layout.OnClose += Close;
        _layout.OpenCentered();
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        base.ReceiveMessage(message);

        if (message is InstrumentUsedAfterInteractEvent msg)
        {
            UpdateLimbsList(msg);
        }
    }
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
    }

    public void UpdateLimbsList(InstrumentUsedAfterInteractEvent msg)
    {
        var sharedBody = _sysMan.GetEntitySystem<SharedBodySystem>();
        var limbs = sharedBody.GetBodyChildren(_entMan.GetEntity(msg.Target)).ToArray();
        
        foreach (var limb in limbs)
        {
            NetEntity netLimb = _entMan.GetNetEntity(limb.Id);
            var button = new SelectorIconButton(netLimb);
            var wrapper = new BoxContainer();
            wrapper.Orientation = BoxContainer.LayoutOrientation.Vertical;
            wrapper.Align = BoxContainer.AlignMode.Center;
            button.Text = _entMan.GetEntityData(netLimb).Item2.EntityName;
            button.OnPressed += args => SendMessage(new SelectorButtonPressed(netLimb));
            _layout!.LimbList.AddChild(wrapper);
            wrapper.AddChild(button);
        }
    }

    public sealed class SelectorIconButton : Button
    {
        public NetEntity TargetId { get; set; }

        public SelectorIconButton(NetEntity targetid)
        {
            TargetId = targetid;
        }
    }
}