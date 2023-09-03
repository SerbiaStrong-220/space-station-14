using Content.Shared.Body.Part;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Surgery.Components
{
    [NetSerializable()]
    public abstract partial class SharedOperapableComponent : Component
    {
        public bool IsOperated = false;

        public byte? CurrentOperation;

        public BodyPartType CurrentOperatedBodyPart;

    }
}
