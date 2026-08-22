using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Pirates;

[Serializable, NetSerializable]
public sealed partial class PirateRecruitmentDoAfterEvent : SimpleDoAfterEvent;
