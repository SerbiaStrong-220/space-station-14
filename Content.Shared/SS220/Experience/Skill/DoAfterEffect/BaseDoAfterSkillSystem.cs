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
    [Dependency] private SharedPopupSystem _popup = default!;

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
    }

    private void OnDoAfterEndInternal(Entity<TComp> entity, ref BeforeDoAfterCompleteEvent args)
    {
        if (args.Args.Event is not TEvent || args.Cancel)
            return;

        if (!SkillEffect(entity, args.Args))
            return;

        OnDoAfterEnd(entity, ref args);

        if (args.Args.Used is null)
            return;

        AfterDoAfterComplete(entity, in args);
    }

    protected virtual void OnDoAfterStart(Entity<TComp> entity, ref BeforeDoAfterStartEvent args)
    {
        if (args.Args.ArgFlags.HasFlag(DoAfterArgFlags.IgnoreExperienceModification))
            return;

        if (!entity.Comp.FullBlock)
        {
            args.Args.DelayModifier *= entity.Comp.DurationScale;
            return;
        }

        args.ShouldCancel = true;

        if (entity.Comp.FullBlockPopup is not null)
            _popup.PopupPredicted(Loc.GetString(entity.Comp.FullBlockPopup), args.Args.Target ?? args.Args.User, args.Args.User, PopupType.MediumCaution);
    }

    protected virtual bool SkillEffect(Entity<TComp> entity, in DoAfterArgs args)
    {
        return true;
    }

    protected virtual void AfterDoAfterComplete(Entity<TComp> entity, in BeforeDoAfterCompleteEvent args) { }

    protected virtual void OnDoAfterEnd(Entity<TComp> entity, ref BeforeDoAfterCompleteEvent args)
    {
        if (args.Args.ArgFlags.HasFlag(DoAfterArgFlags.IgnoreExperienceModification))
            return;

        if (!GetPredictedRandomOnCurTick(GetNetEntity(entity), GetNetEntity(args.Args.User)).Prob(entity.Comp.FailureChance))
            return;

        args.Cancel = true;

        if (entity.Comp.FailurePopup is not null)
            _popup.PopupPredicted(Loc.GetString(entity.Comp.FailurePopup), args.Args.Target ?? args.Args.User, args.Args.User, PopupType.MediumCaution);
    }
}
