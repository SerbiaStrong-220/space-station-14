

namespace Content.Shared.SS220.Attachables;

public abstract partial class AttachableComponent : Component
{
    [DataField]
    public float AttachDelay;

    [DataField]
    public float DeattachDelay;

}
