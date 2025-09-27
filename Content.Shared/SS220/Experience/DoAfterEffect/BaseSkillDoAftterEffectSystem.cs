// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

// THE only reason if DoAfter living in on folder and namespace is it abstract nature to match DoAfterEvents and base functions

using Content.Shared.DoAfter;
using Content.Shared.Popups;
using Content.Shared.SS220.ChangeSpeedDoAfters.Events;
using Content.Shared.SS220.Experience.Systems;
using Robust.Shared.Network;
using Robust.Shared.Random;

namespace Content.Shared.SS220.Experience.DoAfterEffect;

public abstract partial class BaseSkillDoAfterEffectSystem<T1, T2> : EntitySystem where T1 : BaseSkillDoAfterEffectComponent
                                                                                    where T2 : DoAfterEvent
{
    [Dependency] private readonly ExperienceSystem _experience = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly INetManager _netManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<T1, BeforeDoAfterStartEvent>(OnDoAfterStartInternal);
        SubscribeLocalEvent<T1, DoAfterBeforeComplete>(OnDoAfterEndInternal);

        _experience.RelayEventToSkillEntity<T2>();
    }

    private void OnDoAfterStartInternal(Entity<T1> entity, ref BeforeDoAfterStartEvent args)
    {
        if (!args.Args.Event.GetType().Equals(typeof(T2)))
            return;

        OnDoAfterStart(entity, ref args);
    }

    protected void OnDoAfterEndInternal(Entity<T1> entity, ref DoAfterBeforeComplete args)
    {
        if (!args.Args.Event.GetType().Equals(typeof(T2)))
            return;

        OnDoAfterEnd(entity, ref args);
    }

    protected void OnDoAfterStart(Entity<T1> entity, ref BeforeDoAfterStartEvent args)
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

    protected void OnDoAfterEnd(Entity<T1> entity, ref DoAfterBeforeComplete args)
    {
        if (_netManager.IsClient)
            return;

        if (!_robustRandom.Prob(entity.Comp.FailureChance))
            return;

        args.Cancel = true;

        if (entity.Comp.FailurePopup is not null)
            _popup.PopupPredicted(Loc.GetString(entity.Comp.FailurePopup), args.Args.User, args.Args.User, PopupType.SmallCaution);
    }
}
