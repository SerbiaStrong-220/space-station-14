// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Interaction;
using Content.Shared.SS220.AdmemeEvents;
using Robust.Server.GameObjects;
using System.Linq;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.SS220.AdmemeEvents.EventRole;

namespace Content.Server.SS220.AdmemeEvents;

public sealed class JobIconChangerSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;

    private readonly Dictionary<EventRoleIconFilterGroup, string> roleGroupKeys = new()
    {
        { EventRoleIconFilterGroup.IOT, "IronSquad" },
        { EventRoleIconFilterGroup.NT, "SpecOps" },
        { EventRoleIconFilterGroup.USSP, "USSPEbent" }
    };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<JobIconChangerComponent, JobIconChangerChangedMessage>(OnJobChanged);
        SubscribeLocalEvent<JobIconChangerComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<JobIconChangerComponent, BoundUIOpenedEvent>(OnBoundUIOpened);
    }

    private void OnBoundUIOpened(Entity<JobIconChangerComponent> entity, ref BoundUIOpenedEvent args)
    {
        if (!_ui.HasUi(entity, args.UiKey))
            return;

        _ui.SetUiState(entity.Owner, JobIconChangerKey.Key, new JobIconChangerBoundUserInterfaceState(entity.Comp.IconFilterGroup));
    }

    private void OnJobChanged(Entity<JobIconChangerComponent> entity, ref JobIconChangerChangedMessage args)
    {
        if (string.IsNullOrWhiteSpace(args.JobIcon))
            return;

        entity.Comp.JobIcon = args.JobIcon;
    }

    private void OnAfterInteract(Entity<JobIconChangerComponent> entity, ref AfterInteractEvent args)
    {
        if (entity.Comp.CheckReach && !args.CanReach)
            return;

        if (args.Handled || args.Target is not { } target)
            return;

        if (entity.Comp.JobIcon == null)
            return;

        var eventRoleComponent = EnsureComp<EventRoleComponent>(target);

        var jobIcon = entity.Comp.JobIcon.Value;

        if (entity.Comp.IconFilterGroup != EventRoleIconFilterGroup.None)
            eventRoleComponent.RoleGroupKey = roleGroupKeys[entity.Comp.IconFilterGroup];
        else
        {
            //Try getting rolegroup by JobIcon ID :starege:
            //We assume that there's only match (such a hack)
            var roleGroups = roleGroupKeys.Keys.ToList();
            var roleIconFilter = roleGroups.Where(key => jobIcon.Id.StartsWith(key.ToString())).First();
            eventRoleComponent.RoleGroupKey = roleGroupKeys[roleIconFilter];
        }

        if (HasComp<NpcFactionMemberComponent>(args.Target.Value))
        {
            _npcFaction.ClearFactions(args.Target.Value);

            if (entity.Comp.IconFilterGroup == EventRoleIconFilterGroup.IOT)
            {
                _npcFaction.AddFaction(args.Target.Value, "EbentIronSquad");
            }
            if (entity.Comp.IconFilterGroup == EventRoleIconFilterGroup.USSP)
            {
                _npcFaction.AddFaction(args.Target.Value,"EbentUssp");
            }
            if (entity.Comp.IconFilterGroup == EventRoleIconFilterGroup.NT)
            {
                _npcFaction.AddFaction(args.Target.Value,"EbentNanoTrasen");
            }
        }

        eventRoleComponent.StatusIcon = jobIcon;
        args.Handled = true;

        Dirty(target, eventRoleComponent);
    }
}
