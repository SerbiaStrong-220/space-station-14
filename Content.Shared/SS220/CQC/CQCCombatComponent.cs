// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Actions;
using Robust.Shared.Prototypes;
using Content.Shared.SS220.UseableBook;

namespace Content.Shared.SS220.CQCCombat;

[RegisterComponent]
public sealed partial class CQCCombatComponent : Component
{
    // [DataField("hurtMissOn", required: true)]
    // [ViewVariables(VVAccess.ReadWrite)]
    // public float HurtMissOn = default!; // ожидается реализация в модификаторах

    [DataField(required: true)]
    [ViewVariables(VVAccess.ReadWrite)]
    public List<ProtoId<CQCCombatSpellPrototype>> AvailableSpells { get; private set; } = new();
}

public sealed partial class CQCBlowbackEvent : EntityTargetActionEvent { };
public sealed partial class CQCPunchEvent : EntityTargetActionEvent { };
public sealed partial class CQCDisarmEvent : EntityTargetActionEvent { };
public sealed partial class CQCLongSleepEvent : InstantActionEvent { };
public sealed partial class CQCCanReadBook : UseableBookCanReadEvent { };