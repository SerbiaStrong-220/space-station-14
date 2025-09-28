using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.NarcoticsTest;

[Serializable, NetSerializable]
public enum NarcoticsData
{
    Key,
}

[Serializable, NetSerializable]
public sealed partial class CheckNarcoticsDoAfterEvent : SimpleDoAfterEvent;

