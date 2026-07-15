namespace Content.Shared.SS220.Pirates;

[RegisterComponent]
public sealed partial class PirateLootPadComponent : Component;

[RegisterComponent]
public sealed partial class PirateLootConsoleComponent : Component;

[RegisterComponent]
public sealed partial class PirateMarketConsoleComponent : Component;

[RegisterComponent]
public sealed partial class PirateBaseComponent : Component;

[RegisterComponent]
public sealed partial class PirateCrewRoleComponent : Component;

[RegisterComponent]
public sealed partial class PirateRecruitmentContractComponent : Component
{
    [DataField]
    public TimeSpan RecruitmentDelay = TimeSpan.FromSeconds(3);

    [ViewVariables]
    public EntityUid? Offerer;

    [ViewVariables]
    public EntityUid? OfferedTarget;
}

