// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.EntityEffects;
using Content.Shared.Roles;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.DnaLock.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DnaLockableComponent : Component
{
    [DataField, AutoNetworkedField]
    public DnaLockMode Mode = DnaLockMode.InRound;

    [DataField, AutoNetworkedField]
    public ProtoId<JobPrototype>? RoundstartJob;

    [DataField, AutoNetworkedField]
    public HashSet<string> AllowedDna = new();

    [DataField, AutoNetworkedField]
    public bool LockActive;

    [DataField]
    public EntityEffect[] UnauthorizedUseEffects = Array.Empty<EntityEffect>();

    [DataField]
    public LocId? UnauthorizedUsePopup = "dna-lock-unauthorized-popup";

    [DataField]
    public SoundSpecifier? UnauthorizedUseSound = new SoundPathSpecifier("/Audio/Machines/custom_deny.ogg");

    [DataField, AutoNetworkedField]
    public bool Emagged;

    [DataField, AutoNetworkedField]
    public DnaLockBlockedActions BlockedActions = DnaLockBlockedActions.All;
}

public enum DnaLockMode
{
    Roundstart,
    InRound
}

[Flags]
public enum DnaLockBlockedActions : byte
{
    None = 0,
    StorageOpen = 1 << 0,
    ToggleableClothing = 1 << 1,
    BatteryFireModeChange = 1 << 2,
    BatteryEject = 1 << 3,
    All = StorageOpen | ToggleableClothing | BatteryFireModeChange | BatteryEject
}

