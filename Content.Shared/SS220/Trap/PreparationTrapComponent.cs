using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Trap;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class PreparationTrapComponent : Component
{
    [DataField]
    public TimeSpan SetTrapDelay = TimeSpan.FromSeconds(5);

    [DataField]
    public string? TrapProtoType = "lalalala";
}

[Serializable, NetSerializable]
public sealed partial class SetTrapEvent : SimpleDoAfterEvent
{
}
