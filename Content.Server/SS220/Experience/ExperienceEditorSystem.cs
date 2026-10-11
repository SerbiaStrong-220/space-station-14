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

public sealed partial class ExperienceEditorSystem : EntitySystem
{
    [Dependency] private IAdminLogManager _adminLog = default!;
    [Dependency] private IConGroupController _groupController = default!;
    [Dependency] private ExperienceAdditionSystem _experienceAddition = default!;

    private const string EditorCommand = "expeditor";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<ChangeEntityExperienceAdminRequest>(OnChangeAdminRequest);
        SubscribeNetworkEvent<ChangeEntityExperiencePlayerRequest>(OnChangePlayerRequest);

        SubscribeLocalEvent<RoundEndedEvent>(OnRoundEnd);
    }

    private void OnChangeAdminRequest(ChangeEntityExperienceAdminRequest ev, EntitySessionEventArgs args)
    {
        if (!_groupController.CanCommand(args.SenderSession, EditorCommand))
            return;

        var targetEntity = GetEntity(ev.Target);
        if (!TryComp<ExperienceComponent>(targetEntity, out var targetEntityExperienceComp))
        {
            Log.Error($"Tried to change {nameof(ExperienceComponent)} of entity which don't have one, entity is {ToPrettyString(targetEntity)}!");
            return;
        }

        var experienceAddition = EnsureComp<ExperienceAdditionComponent>(targetEntity);

        if (ev.Data is not { } additionData)
        {
            _experienceAddition.RemoveAdminAdded(targetEntity);
            return;
        }

        var correctSkillDict = additionData.SkillDictionary.Values
            .SelectMany(list => list)
            .ToDictionary(view => view.SkillTreeId, item => item.Info);

        // TODO: make forced definable by admin
        _experienceAddition.UpsertAdminForcedSpecifier(targetEntity, ExperienceAddSource.Admin, new ExperienceAddSpecifier(0, additionData.Knowledges, correctSkillDict));
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
            Dirty(playerEntity, experienceComponent);
            return;
        }

        var playerChangedComp = EnsureComp<JobBackgroundSublevelAddComponent>(playerEntity);

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
        Dirty(playerEntity, playerChangedComp);

        _experienceAddition.EnqueueRecalculation(playerEntity);
    }

    private void OnRoundEnd(RoundEndedEvent _)
    {
        var backgroundEntityQuery = EntityQueryEnumerator<JobBackgroundSublevelAddComponent, ExperienceAdditionComponent>();

        while (backgroundEntityQuery.MoveNext(out var uid, out var sublevelAddComponent, out var additionComponent))
        {
            var stringSublevelView = GetSublevelStringView(sublevelAddComponent.Skills);

            _adminLog.Add(LogType.Experience, LogImpact.Low, $"At round end entity with experience definition {additionComponent.DefinitionId}, used {sublevelAddComponent.SpentSublevelPoints} point to their leveling. Used to skills {stringSublevelView}");
        }
    }

    private string GetSublevelStringView(Dictionary<ProtoId<SkillTreePrototype>, int> toView)
    {
        return string.Join('|', toView.Select(x => $"{x.Key}: {x.Value}"));
    }
}
