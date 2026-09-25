using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Pirates;

[Serializable, NetSerializable]
public sealed class PirateRecruitmentChoiceMessage(bool accepted) : EuiMessageBase
{
    public readonly bool Accepted = accepted;
}
