// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.EntityEffects;
using Content.Shared.Roles;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.EquipmentDnaLock.Components;

public enum EquipmentDnaLockMode
{
    Roundstart,
    InRound
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class EquipmentDnaLockComponent : Component
{

    [DataField, AutoNetworkedField]
    public EquipmentDnaLockMode Mode = EquipmentDnaLockMode.InRound;

    [DataField, AutoNetworkedField]
    public bool RequireSwitchableWeaponForHandUse = true;

    [DataField, AutoNetworkedField]
    public ProtoId<JobPrototype>? RoundstartJob;

    [DataField, AutoNetworkedField]
    public HashSet<string> AllowedDna = new();

    [DataField, AutoNetworkedField]
    public bool LockActive;

    [DataField]
    public EntityEffect[] UnauthorizedUseEffects = Array.Empty<EntityEffect>();

    [DataField]
    public LocId? UnauthorizedUsePopup = "equipment-dna-lock-unauthorized-popup";

    [DataField]
    public SoundSpecifier? UnauthorizedUseSound = new SoundPathSpecifier("/Audio/Machines/custom_deny.ogg");

    [DataField, AutoNetworkedField]
    public bool Emagged;

    [DataField, AutoNetworkedField]
    public bool BlockStorageOpen = true;

    [DataField, AutoNetworkedField]
    public bool BlockToggleableClothing = true;

    [DataField, AutoNetworkedField]
    public bool BlockBatteryFireModeChange = true;

    [DataField, AutoNetworkedField]
    public bool BlockBatteryEject = true;
}
