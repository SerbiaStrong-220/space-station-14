using Content.Shared.Access.Systems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Roles;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Access.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedIdCardConsoleSystem))]
public sealed partial class IdCardConsoleComponent : Component
{
    public static string PrivilegedIdCardSlotId = "IdCardConsole-privilegedId";
    public static string TargetIdCardSlotId = "IdCardConsole-targetId";

    [DataField]
    public ItemSlot PrivilegedIdSlot = new();

    [DataField]
    public ItemSlot TargetIdSlot = new();

    [Serializable, NetSerializable]
    public sealed class WriteToTargetIdMessage : BoundUserInterfaceMessage
    {
        public readonly string FullName;
        public readonly string JobTitle;
        public readonly List<ProtoId<AccessLevelPrototype>> AccessList;
        public readonly ProtoId<JobPrototype> JobPrototype;

        public WriteToTargetIdMessage(string fullName, string jobTitle, List<ProtoId<AccessLevelPrototype>> accessList, ProtoId<JobPrototype> jobPrototype)
        {
            FullName = fullName;
            JobTitle = jobTitle;
            AccessList = accessList;
            JobPrototype = jobPrototype;
        }
    }

    // Put this on shared so we just send the state once in PVS range rather than every time the UI updates.

    [DataField, AutoNetworkedField]
    public List<ProtoId<AccessLevelPrototype>> AccessLevels = new()
    {
        "Armory",
        "Atmospherics",
        "Bar",
        "Brig",
        "Blueshield",
        "Detective",
        "Captain",
        "Cargo",
        "Chapel",
        "Chemistry",
        "ChiefEngineer",
        "ChiefMedicalOfficer",
        "Paramedic",
        "Command",
        "Cryogenics",
        "Clown", //SS220 New_accesses_and_some_tweaks
        "Engineering",
        "External",
        "HeadOfPersonnel",
        "HeadOfSecurity",
        "Hydroponics",
        "Janitor",
        "Kitchen",
        "Lawyer",
        "Librarian", //SS220 New_accesses_and_some_tweaks
        "Maintenance",
        "Medical",
        "Psychologist", //SS220 New_accesses_and_some_tweaks
        "Quartermaster",
        "Research",
        "ResearchDirector",
        "Reporter", //SS220 New_accesses_and_some_tweaks
        "Salvage",
        "Security",
        "Service",
        "Theatre",
        "Magistrate", //SS220-Magistrate Access Fix
        "Mime", //SS220 New_accesses_and_some_tweaks
        "Musician", //SS220 New_accesses_and_some_tweaks
    };

    // SS220-ID console extended access button-Begin
    /// <summary>
    /// All access levels which will be given to ID card when "Extended" button is pressed
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<ProtoId<AccessLevelPrototype>> ExtendedAccessLevels = new()
    {
        "Bar",
        "Cargo",
        "Chapel",
        "Paramedic",
        "Cryogenics",
        "Engineering",
        "External",
        "Hydroponics",
        "Janitor",
        "Kitchen",
        "Lawyer",
        "Maintenance",
        "Medical",
        "Research",
        "Salvage",
        "Service",
        "Theatre",
    };
    // SS220-ID console extended access button-End

    [Serializable, NetSerializable]
    public sealed class IdCardConsoleBoundUserInterfaceState : BoundUserInterfaceState
    {
        public readonly string PrivilegedIdName;
        public readonly bool IsPrivilegedIdPresent;
        public readonly bool IsPrivilegedIdAuthorized;
        public readonly bool IsTargetIdPresent;
        public readonly string TargetIdName;
        public readonly string? TargetIdFullName;
        public readonly string? TargetIdJobTitle;
        public readonly List<ProtoId<AccessLevelPrototype>>? TargetIdAccessList;
        public readonly List<ProtoId<AccessLevelPrototype>>? AllowedModifyAccessList;
        public readonly ProtoId<JobPrototype> TargetIdJobPrototype;

        public IdCardConsoleBoundUserInterfaceState(bool isPrivilegedIdPresent,
            bool isPrivilegedIdAuthorized,
            bool isTargetIdPresent,
            string? targetIdFullName,
            string? targetIdJobTitle,
            List<ProtoId<AccessLevelPrototype>>? targetIdAccessList,
            List<ProtoId<AccessLevelPrototype>>? allowedModifyAccessList,
            ProtoId<JobPrototype> targetIdJobPrototype,
            string privilegedIdName,
            string targetIdName)
        {
            IsPrivilegedIdPresent = isPrivilegedIdPresent;
            IsPrivilegedIdAuthorized = isPrivilegedIdAuthorized;
            IsTargetIdPresent = isTargetIdPresent;
            TargetIdFullName = targetIdFullName;
            TargetIdJobTitle = targetIdJobTitle;
            TargetIdAccessList = targetIdAccessList;
            AllowedModifyAccessList = allowedModifyAccessList;
            TargetIdJobPrototype = targetIdJobPrototype;
            PrivilegedIdName = privilegedIdName;
            TargetIdName = targetIdName;
        }
    }

    [Serializable, NetSerializable]
    public enum IdCardConsoleUiKey : byte
    {
        Key,
    }
}
