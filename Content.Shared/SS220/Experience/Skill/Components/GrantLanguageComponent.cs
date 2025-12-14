// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Language;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Experience.Skill.Components;

[RegisterComponent]
[NetworkedComponent]
public sealed partial class GrantLanguageComponent : Component
{
    [DataField(required: true)]
    public HashSet<ProtoId<LanguagePrototype>> Languages = [];

    [DataField]
    public bool CanSpeak = true;
}
