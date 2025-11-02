using Content.Server.Administration.Logs;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Server.Roles;
using Content.Server.SS220.MindSlave;
using Content.Shared.Database;
using Content.Shared.Implants;
using Content.Shared.Mindshield.Components;
using Content.Shared.Revolutionary.Components;
using Content.Shared.Roles.Components;
using Content.Shared.SS220.CombustingMindShield;
using Content.Shared.SS220.CultYogg.Cultists;
using Robust.Shared.Containers;

namespace Content.Server.Mindshield;

/// <summary>
/// System used for adding or removing components with a mindshield implant
/// as well as checking if the implanted is a Rev or Head Rev.
/// </summary>
public sealed class MindShieldSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLogManager = default!;
    [Dependency] private readonly RoleSystem _roleSystem = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly MindSlaveSystem _mindSlave = default!;
    [Dependency] private readonly SharedSubdermalImplantSystem _sharedSubdermalImplant = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MindShieldImplantComponent, ImplantImplantedEvent>(OnImplantImplanted);
        SubscribeLocalEvent<MindShieldImplantComponent, ImplantRemovedEvent>(OnImplantRemoved);
    }

    private void OnImplantImplanted(Entity<MindShieldImplantComponent> ent, ref ImplantImplantedEvent ev)
    {
        if (ev.Implanted == null)
            return;

        EnsureComp<MindShieldComponent>(ev.Implanted);
        MindShieldRemovalCheck(ev.Implanted, ev.Implant);
    }

    /// <summary>
    /// Checks if the implanted person was a Rev or Head Rev and remove role or destroy mindshield respectively.
    /// </summary>
    private void MindShieldRemovalCheck(EntityUid implanted, EntityUid implant)
    {
        if (HasComp<HeadRevolutionaryComponent>(implanted))
        {
            _popupSystem.PopupEntity(Loc.GetString("head-rev-break-mindshield"), implanted);
            QueueDel(implant);
            return;
        }

        if (_mindSystem.TryGetMind(implanted, out var mindId, out _) &&
            _roleSystem.MindRemoveRole<RevolutionaryRoleComponent>(mindId))
        {
            _adminLogManager.Add(LogType.Mind, LogImpact.Medium, $"{ToPrettyString(implanted)} was deconverted due to being implanted with a Mindshield.");
        }
        //SS220 Cult hotfix 14 #3477 start
        if (HasComp<CultYoggComponent>(implanted))
        {
            var comp = EnsureComp<CombustingMindShieldComponent>(implanted);
            comp.Implant = implant;
        }
        //SS220 Cult hotfix 14 #3477 end 
    }

    private void OnImplantRemoved(Entity<MindShieldImplantComponent> ent, ref ImplantRemovedEvent args)
    {
        RemComp<MindShieldComponent>(args.Implanted);
    }
}

