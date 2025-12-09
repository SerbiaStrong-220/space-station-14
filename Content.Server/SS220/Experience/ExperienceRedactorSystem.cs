// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using Content.Shared.SS220.Experience;
using Robust.Server.Console;

namespace Content.Server.SS220.Experience;

public sealed class ExperienceRedactorSystem : EntitySystem
{
    [Dependency] private readonly IConGroupController _groupController = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<ChangeEntityExperienceRequest>(OnChangeRequest);
    }

    private void OnChangeRequest(ChangeEntityExperienceRequest ev, EntitySessionEventArgs args)
    {
        if (!_groupController.CanCommand(args.SenderSession, "experienceredactor"))
            return;

        var targetEntity = GetEntity(ev.Target);
        if (!HasComp<ExperienceComponent>(targetEntity))
        {
            Log.Error($"Tried to change {nameof(ExperienceComponent)} of entity which don't have one, entity is {ToPrettyString(targetEntity)}!");
            return;
        }

        var forceAddComponent = EnsureComp<SkillForcedAddComponent>(targetEntity);

        forceAddComponent.Knowledges = ev.Data.Knowledges;
        forceAddComponent.Skills = ev.Data.SkillDictionary.Values
            .SelectMany(list => list)
            .ToDictionary(item => item.Item1, item => item.Item2.Info);

        var afterGainedEv = new AfterExperienceInitComponentGained(InitGainedExperienceType.AdminForced);
        RaiseLocalEvent(targetEntity, ref afterGainedEv);
    }
}
