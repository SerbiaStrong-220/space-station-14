// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

// THE only reason if DoAfter living in on folder and namespace is it abstract nature to match DoAfterEvents and base functions

using Content.Shared.DoAfter;
using Content.Shared.SS220.ChangeSpeedDoAfters.Events;
using Content.Shared.SS220.Experience.Systems;

namespace Content.Shared.SS220.Experience.DoAfterEffect;

public abstract partial class BaseSkillDoAfterEffectSystem<T1, T2> : EntitySystem where T1 : BaseSkillDoAfterEffectComponent
                                                                                    where T2 : DoAfterEvent
{
    [Dependency] private readonly ExperienceSystem _experience = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<T1, BeforeDoAfterStartEvent>(OnDoAfterStart);
        SubscribeLocalEvent<T1, T2>(OnDoAfterEnd);

        _experience.SubscribeSkillEntityToEvent<T2>();
    }

    protected void OnDoAfterStart(Entity<T1> entity, ref BeforeDoAfterStartEvent args)
    {
        if (!args.Args.Event.GetType().Equals(typeof(T2)))
            return;


    }
    protected void OnDoAfterEnd(Entity<T1> entity, ref T2 args)
    {
        if (!args.Args.Event.GetType().Equals(typeof(T2)))
            return;
    }
}
