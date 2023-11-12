// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Linq;
using Content.Shared.Body.Systems;
using Content.Shared.SS220.Surgery.Systems;
using JetBrains.Annotations;
using Linguini.Syntax.Ast;
using Linguini.Syntax.Serialization;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client.SS220.Surgery.Selector
{
    [UsedImplicitly]
    public sealed partial class LimbSelectorBoundUserInterface : BoundUserInterface
    {
        [Dependency] private readonly IEntitySystemManager _sysMan = default!;
        [Dependency] private readonly EntityManager _entMan = default!;
        public LimbSelectorLayout? _layout;
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
                UpdateTarget(msg);
            }
        }
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        public void UpdateTarget(InstrumentUsedAfterInteractEvent msg)
        {
            var sharedBody = _sysMan.GetEntitySystem<SharedBodySystem>();
            var limbs = sharedBody.GetBodyChildren(_entMan.GetEntity(msg.Target)).ToArray();

            foreach (var limb in limbs)
            {
                NetEntity netLimb = _entMan.GetNetEntity(limb.Id);
                var button = new LimbIconButton(netLimb);
                button.Text = _entMan.GetEntityData(netLimb).Item2.EntityName;
                button.OnPressed += args => SendMessage(new SelectorButtonPressed(netLimb));

                _layout!.LimbList.AddChild(button);
            }
        }

        public sealed class LimbIconButton : Button
        {
            public NetEntity LimbId { get; set; }

            public LimbIconButton(NetEntity limbid)
            {
                LimbId = limbid;
            }
        }
    }
}