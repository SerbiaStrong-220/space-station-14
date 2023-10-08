using Content.Server.Mind.Components;
using Content.Server.Objectives;
using Content.Server.Roles;
using Content.Shared.CharacterInfo;
using Content.Shared.Objectives;

namespace Content.Server.CharacterInfo;

public sealed class CharacterInfoSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<RequestCharacterInfoEvent>(OnRequestCharacterInfoEvent);
        SubscribeNetworkEvent<RequestAntagonistInfoEvent>(OnRequestAntagonistInfoEvent);
    }

    private void OnRequestCharacterInfoEvent(RequestCharacterInfoEvent msg, EntitySessionEventArgs args)
    {
        if (!args.SenderSession.AttachedEntity.HasValue || args.SenderSession.AttachedEntity != msg.EntityUid)
            return;

        var entity = args.SenderSession.AttachedEntity.Value;

        var jobTitle = "No Profession";
        var conditions = new Dictionary<string, List<ConditionInfo>>();
        var briefing = "!!ERROR: No Briefing!!"; //should never show on the UI unless there's a bug

        if (EntityManager.TryGetComponent(entity, out MindContainerComponent? mindContainerComponent) && mindContainerComponent.Mind != null)
        {
            var mind = mindContainerComponent.Mind;

            GetJobTitle(jobTitle, mind.AllRoles);

            GetConditions(conditions, mind.AllObjectives);

            // Get briefing
            briefing = mind.Briefing;
        }

        RaiseNetworkEvent(new CharacterInfoEvent(entity, jobTitle, conditions, briefing), args.SenderSession);
    }

    private void OnRequestAntagonistInfoEvent(RequestAntagonistInfoEvent msg, EntitySessionEventArgs args)
    {
        if (!args.SenderSession.AttachedEntity.HasValue)
            return;

        var receiver = args.SenderSession.AttachedEntity.Value;
        var antagonist = msg.EntityUid;

        var jobTitle = "No Profession";
        var conditions = new Dictionary<string, List<ConditionInfo>>();

        if (EntityManager.TryGetComponent(antagonist, out MindContainerComponent? mindContainerComponent) && mindContainerComponent.Mind != null)
        {
            var mind = mindContainerComponent.Mind;

            GetJobTitle(jobTitle, mind.AllRoles);

            GetConditions(conditions, mind.AllObjectives);
        }

        RaiseNetworkEvent(new AntagonistInfoEvent(receiver, antagonist, jobTitle, conditions), args.SenderSession);
    }

    private void GetJobTitle(string jobTitle, IEnumerable<Role> roles)
    {
        // Get job title
        foreach (var role in roles)
        {
            if (role.GetType() != typeof(Job)) continue;

            jobTitle = role.Name;
            break;
        }
    }

    private void GetConditions(Dictionary<string, List<ConditionInfo>> conditions, IEnumerable<Objective> objectives)
    {
        // Get objectives
        foreach (var objective in objectives)
        {
            if (!conditions.ContainsKey(objective.Prototype.Issuer))
                conditions[objective.Prototype.Issuer] = new List<ConditionInfo>();
            foreach (var condition in objective.Conditions)
            {
                conditions[objective.Prototype.Issuer].Add(new ConditionInfo(condition.Title,
                    condition.Description, condition.Icon, condition.Progress));
            }
        }
    }
}
