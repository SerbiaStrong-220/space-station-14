using Content.Shared.Body.Part;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Surgery.Components
{
    [NetSerializable()]
    public abstract partial class SharedOperapableComponent : Component
    {
        [DataField("isoperated")]
        public bool IsOperated = false;

        [DataField("isopened")]
        public bool IsOpened = false;

        [DataField("operationcurr")]
        public byte? CurrentOperation;

        [DataField("bodypart")]
        public BodyPartType CurrentOperatedBodyPart;

    }
}
