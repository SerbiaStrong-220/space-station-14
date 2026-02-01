// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

// THE only reason if DoAfter living in own folder and namespace is it abstract nature to match DoAfterEvents and base functions

using System.Diagnostics.CodeAnalysis;
using Content.Shared.DoAfter;
using Content.Shared.Popups;
using Content.Shared.SS220.ChangeSpeedDoAfters.Events;
using Content.Shared.SS220.Experience.Skill;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.SS220.Experience.DoAfterEffect;

public abstract partial class BaseDoAfterSkillSystem<TComp, TEvent> : SkillEntitySystem where TComp : BaseDoAfterSkillComponent
                                                                                    where TEvent : DoAfterEvent
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeEventToSkillEntity<TComp, BeforeDoAfterStartEvent>(OnDoAfterStartInternal);
        SubscribeEventToSkillEntity<TComp, BeforeDoAfterCompleteEvent>(OnDoAfterEndInternal);
    }

    private void OnDoAfterStartInternal(Entity<TComp> entity, ref BeforeDoAfterStartEvent args)
    {
        if (args.Args.Event is not TEvent)
            return;

        if (!SkillEffect(entity, args.Args))
            return;

        OnDoAfterStart(entity, ref args);

        if (args.ShouldCancel || args.Args.Used == null)
            return;

        if (!TryGetLearningProgressInfo<LearningOnDoAfterStartWithComponent>(args.Args.Used.Value, entity.Comp.SkillTreeGroup, out var learningInformation))
            return;

        TryChangeStudyingProgress(entity, entity.Comp.SkillTreeGroup, learningInformation.Value);
    }

    private void OnDoAfterEndInternal(Entity<TComp> entity, ref BeforeDoAfterCompleteEvent args)
    {
        if (args.Args.Event is not TEvent)
            return;

        if (!SkillEffect(entity, args.Args))
            return;

        OnDoAfterEnd(entity, ref args);

        if (args.Cancel || args.Args.Used is null)
            return;

        if (!LearnedAfterComplete(in args))
            return;

        AfterDoAfterComplete(entity, in args);

        if (!TryGetLearningProgressInfo<LearningOnDoAfterEndWithComponent>(args.Args.Used.Value, entity.Comp.SkillTreeGroup, out var learningInformation))
            return;

        TryChangeStudyingProgress(entity, entity.Comp.SkillTreeGroup, learningInformation.Value);
    }

    protected virtual void OnDoAfterStart(Entity<TComp> entity, ref BeforeDoAfterStartEvent args)
    {
        if (!entity.Comp.FullBlock)
        {
            args.Args.Delay *= entity.Comp.DurationScale;
            return;
        }

        args.ShouldCancel = true;

        if (entity.Comp.FullBlockPopup is not null)
            _popup.PopupClient(Loc.GetString(entity.Comp.FullBlockPopup), args.Args.User);
    }

    protected virtual bool SkillEffect(Entity<TComp> entity, in DoAfterArgs args)
    {
        return true;
    }

    protected virtual bool LearnedAfterComplete(in BeforeDoAfterCompleteEvent args)
    {
        return true;
    }

    protected virtual void AfterDoAfterComplete(Entity<TComp> entity, in BeforeDoAfterCompleteEvent args) { }

    protected virtual void OnDoAfterEnd(Entity<TComp> entity, ref BeforeDoAfterCompleteEvent args)
    {
        if (!GetPredictedRandomOnCurTick(new() { GetNetEntity(entity).Id, GetNetEntity(args.Args.User).Id }).Prob(entity.Comp.FailureChance))
            return;

        args.Cancel = true;

        if (entity.Comp.FailurePopup is not null)
            _popup.PopupEntity(Loc.GetString(entity.Comp.FailurePopup), args.Args.User, args.Args.User, PopupType.SmallCaution);
    }

    private bool TryGetLearningProgressInfo<T>(Entity<T?> entity, ProtoId<SkillTreePrototype>? treeId, [NotNullWhen(true)] out LearningInformation? learningInformation) where T : BaseLearningOnDoAfterWithComponent
    {
        learningInformation = null;

        if (treeId is null)
            return false;

        if (!Resolve(entity.Owner, ref entity.Comp, logMissing: false))
            return false;

        if (!entity.Comp.Progress.TryGetValue(treeId.Value, out var learningInformationTemp))
            return false;

        learningInformation = learningInformationTemp;
        return true;
    }
}
