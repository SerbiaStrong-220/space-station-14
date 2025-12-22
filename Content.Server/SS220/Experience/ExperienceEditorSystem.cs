// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.GameTicking;
using Content.Shared.SS220.Experience;
using Content.Shared.SS220.Experience.Systems;
using Robust.Server.Console;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Server.SS220.Experience;

public sealed class ExperienceEditorSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IConGroupController _groupController = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<ChangeEntityExperienceAdminRequest>(OnChangeAdminRequest);
        SubscribeNetworkEvent<ChangeEntityExperiencePlayerRequest>(OnChangePlayerRequest);
        SubscribeLocalEvent<RoundEndedEvent>(OnRoundEnd);
    }

    private void OnChangeAdminRequest(ChangeEntityExperienceAdminRequest ev, EntitySessionEventArgs args)
    {
        if (!_groupController.CanCommand(args.SenderSession, "expeditor"))
            return;

        var targetEntity = GetEntity(ev.Target);
        if (!HasComp<ExperienceComponent>(targetEntity))
        {
            Log.Error($"Tried to change {nameof(ExperienceComponent)} of entity which don't have one, entity is {ToPrettyString(targetEntity)}!");
            return;
        }

        var forceAddComponent = EnsureComp<AdminForcedExperienceAddComponent>(targetEntity);

        forceAddComponent.Knowledges = ev.Data.Knowledges;
        forceAddComponent.Skills = ev.Data.SkillDictionary.Values
            .SelectMany(list => list)
            .ToDictionary(view => view.SkillTreeId, item => item.Info);

        var afterGainedEv = new RecalculateEntityExperience();
        RaiseLocalEvent(targetEntity, ref afterGainedEv);
    }

    private void OnChangePlayerRequest(ChangeEntityExperiencePlayerRequest ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } playerEntity)
            return;

        if (!TryComp<ExperienceComponent>(playerEntity, out var experienceComponent))
            return;

        var validInput = ev.ChangeSkill.SkillSublevels.Where(x => x.Value >= 0).ToDictionary();
        var totalPointsSpend = validInput.Values.Sum();

        if (experienceComponent.FreeSublevelPoints < totalPointsSpend)
        {
            DirtyField(playerEntity, experienceComponent, nameof(ExperienceComponent.FreeSublevelPoints));
            return;
        }

        var playerChangedComp = EnsureComp<BackgroundSublevelAddComponent>(playerEntity);

        foreach (var (skillId, sublevel) in validInput)
        {
            if (!playerChangedComp.Skills.TryGetValue(skillId, out var oldSublevel))
            {
                oldSublevel = ExperienceSystem.StartSublevel;
                playerChangedComp.Skills.Add(skillId, oldSublevel);
            }

            playerChangedComp.Skills[skillId] += (sublevel - ExperienceSystem.StartSublevel);
        }

        _adminLog.Add(LogType.Experience, LogImpact.Low, $"{ToPrettyString(playerEntity):user} used free points for adding sublevels. Used to {GetSublevelStringView(validInput)}");

        playerChangedComp.SpentSublevelPoints += totalPointsSpend;

        var afterInitEv = new RecalculateEntityExperience();
        RaiseLocalEvent(playerEntity, ref afterInitEv);

        DirtyField(playerEntity, experienceComponent, nameof(ExperienceComponent.FreeSublevelPoints));
    }

    private void OnRoundEnd(RoundEndedEvent _)
    {
        var backgroundEntityQuery = EntityQueryEnumerator<BackgroundSublevelAddComponent, RoleExperienceAddComponent>();

        while (backgroundEntityQuery.MoveNext(out var uid, out var sublevelAddComponent, out var roleExperience))
        {
            var stringSublevelView = GetSublevelStringView(sublevelAddComponent.Skills);

            _adminLog.Add(LogType.Experience, LogImpact.Low, $"At round end entity with experience definition {roleExperience.DefinitionId}, used {sublevelAddComponent.SpentSublevelPoints} point to their leveling. Used to skills {stringSublevelView}");
        }
    }

    private string GetSublevelStringView(Dictionary<ProtoId<SkillTreePrototype>, int> toView)
    {
        return string.Join('|', toView.Select(x => $"{x.Key}: {x.Value}"));
    }
}
