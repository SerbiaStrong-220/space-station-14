// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

// THE only reason if DoAfter living in on folder and namespace is it abstract nature to match DoAfterEvents and base functions

using System.Diagnostics.CodeAnalysis;
using Content.Shared.DoAfter;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.SS220.ChangeSpeedDoAfters.Events;
using Content.Shared.SS220.Experience.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.SS220.Experience.DoAfterEffect;

public abstract partial class BaseSkillDoAfterEffectSystem<T1, T2> : EntitySystem where T1 : BaseSkillDoAfterEffectComponent
                                                                                    where T2 : DoAfterEvent
{
    [Dependency] protected readonly ExperienceSystem Experience = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public readonly Color FasterDoAfterBarColor = Color.FromHex("#ffd726ff");
    public readonly Color SlowerDoAfterBarColor = Color.FromHex("#8d83d2ff");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<T1, BeforeDoAfterStartEvent>(OnDoAfterStartInternal);
        SubscribeLocalEvent<T1, DoAfterBeforeComplete>(OnDoAfterEndInternal);

        Experience.RelayEventToSkillEntity<T1, BeforeDoAfterStartEvent>();
        Experience.RelayEventToSkillEntity<T1, DoAfterBeforeComplete>();
    }

    private void OnDoAfterStartInternal(Entity<T1> entity, ref BeforeDoAfterStartEvent args)
    {
        if (!args.Args.Event.GetType().Equals(typeof(T2)))
            return;

        OnDoAfterStart(entity, ref args);

        if (args.ShouldCancel || args.Args.Used == null)
            return;

        if (!Experience.TryGetExperienceEntityFromSkillEntity(entity.Owner, out var experienceEntity))
        {
            Log.Error($"Cant get owner of skill entity {ToPrettyString(entity)}");
            return;
        }

        if (!TryGetLearningProgressInfo<LearningOnDoAfterStartWithComponent>(args.Args.Used.Value, entity.Comp.SkillTreeGroup, out var learningInformation))
            return;

        Experience.TryChangeStudyingProgress(experienceEntity.Value.Owner, entity.Comp.SkillTreeGroup, learningInformation.Value);
    }

    private void OnDoAfterEndInternal(Entity<T1> entity, ref DoAfterBeforeComplete args)
    {
        if (!args.Args.Event.GetType().Equals(typeof(T2)))
            return;

        OnDoAfterEnd(entity, ref args);

        if (args.Cancel || args.Args.Used == null)
            return;

        if (!Experience.TryGetExperienceEntityFromSkillEntity(entity.Owner, out var experienceEntity))
        {
            Log.Error($"Cant get owner of skill entity {ToPrettyString(entity)}");
            return;
        }

        if (!TryGetLearningProgressInfo<LearningOnDoAfterEndWithComponent>(args.Args.Used.Value, entity.Comp.SkillTreeGroup, out var learningInformation))
            return;

        Experience.TryChangeStudyingProgress(experienceEntity.Value.Owner, entity.Comp.SkillTreeGroup, learningInformation.Value);
    }

    protected virtual void OnDoAfterStart(Entity<T1> entity, ref BeforeDoAfterStartEvent args)
    {
        if (!entity.Comp.FullBlock)
        {
            args.Args.Delay *= entity.Comp.DurationScale;
            args.Args.BarColorOverride = entity.Comp.DurationScale switch
            {
                < 1f => FasterDoAfterBarColor,
                > 1f => SlowerDoAfterBarColor,
                _ => null
            };

            return;
        }

        args.ShouldCancel = true;

        if (entity.Comp.FullBlockPopup is not null)
            _popup.PopupClient(Loc.GetString(entity.Comp.FullBlockPopup), args.Args.User);
    }

    protected virtual void OnDoAfterEnd(Entity<T1> entity, ref DoAfterBeforeComplete args)
    {
        // TODO: Once we have predicted randomness delete this for something sane...
        var seed = SharedRandomExtensions.HashCodeCombine(new() { (int)_timing.CurTick.Value, GetNetEntity(entity).Id, GetNetEntity(args.Args.User).Id });
        var rand = new System.Random(seed);

        if (!rand.Prob(entity.Comp.FailureChance))
            return;

        args.Cancel = true;

        if (entity.Comp.FailurePopup is not null)
            _popup.PopupPredicted(Loc.GetString(entity.Comp.FailurePopup), args.Args.User, args.Args.User, PopupType.SmallCaution);
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
