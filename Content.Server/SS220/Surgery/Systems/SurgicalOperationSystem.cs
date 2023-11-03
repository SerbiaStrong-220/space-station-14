
using Content.Server.SS220.Surgery.Components;

namespace Content.Server.SS220.Surgery.Systems
{
    public sealed partial class SurgicalOperationSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
        }

        public bool TryMakeIncision(EntityUid limb, SurgicalInstrumentComponent instrumentComp)
        {
            return true;
        }
    }
}
