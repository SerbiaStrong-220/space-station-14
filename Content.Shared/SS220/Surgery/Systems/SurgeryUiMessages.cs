
using Content.Shared.Body.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Surgery.Systems
{
    [Serializable, NetSerializable]
    public sealed partial class InstrumentUsedAfterInteractEvent : BoundUserInterfaceMessage
    {
        public NetEntity Target { get; set; }

        public InstrumentUsedAfterInteractEvent(NetEntity target)
        {
            Target = target;
        }
    }

    [Serializable, NetSerializable]
    public sealed partial class SelectorButtonPressed : BoundUserInterfaceMessage
    {
        public NetEntity LimbId { get; set; }

        public SelectorButtonPressed(NetEntity limbid)
        {
            LimbId = limbid;
        }
    }
}