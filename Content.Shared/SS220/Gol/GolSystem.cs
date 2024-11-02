using Robust.Shared.Audio.Systems;
using Content.Shared.Actions;

namespace Content.Shared.SS220.Gol;

public sealed class GolSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GolComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<GolComponent, GolActionEvent>(OnGolAction);
    }

    private void OnMapInit(EntityUid uid, GolComponent component, MapInitEvent args)
    {
        _actions.AddAction(uid, ref component.GolActionEntity, component.GolAction);
    }
    private void OnGolAction(EntityUid uid, GolComponent component, GolActionEvent args)
    {
        if (args.Handled)
            return;

        _audio.PlayPredicted(component.GolSound, uid, uid);
    }
}
