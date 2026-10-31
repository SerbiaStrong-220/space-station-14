using Content.Shared.CartridgeLoader;

namespace Content.Shared.SS220.Cartridges.Timer;

public abstract partial class SharedTimerCartridgeSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TimerCartridgeComponent, CartridgeAddedEvent>(OnCartridgeAdded);
    }

    private void OnCartridgeAdded(EntityUid uid, TimerCartridgeComponent comp, ref CartridgeAddedEvent ev)
    {
        EnsureComp<TimerCartridgeInteractionComponent>(ev.Loader);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<TimerCartridgeComponent>();

        while (query.MoveNext(out var uid, out var timer))
        {
            if (!timer.TimerActive)
                continue;

            timer.Timer -= TimeSpan.FromSeconds(frameTime);

            if (timer.Timer < TimeSpan.Zero)
            {
                EndTimer(uid, comp: timer);
            }
        }
    }

    public abstract void EndTimer(EntityUid uid, bool notify = true, TimerCartridgeComponent? comp = null);
}
