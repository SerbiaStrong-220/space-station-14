// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Actions;
using Robust.Shared.Prototypes;
using Content.Shared.SS220.UseableBook;

namespace Content.Shared.SS220.CQCCombat;

[RegisterComponent]
public sealed partial class CQCCombatInfosComponent : Component
{
    public string Prototype {get;}
    public CQCCombatInfosComponent(string proto)
    {
        Prototype = proto;
    }
}

public sealed partial class CQCBlowbackEvent : EntityTargetActionEvent { };
public sealed partial class CQCPunchEvent : EntityTargetActionEvent { };
public sealed partial class CQCDisarmEvent : EntityTargetActionEvent { };
public sealed partial class CQCLongSleepEvent : InstantActionEvent { };
public sealed partial class CQCCanReadBook : UseableBookCanReadEvent { };