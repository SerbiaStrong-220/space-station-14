// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Actions;
using Content.Shared.SS220.IgnoreLightVision.Components;
using Content.Shared.SS220.Virology.Behaviors;

namespace Content.Server.SS220.Virology.Behaviors;

public sealed partial class VirusSharpHearingSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VirusSharpHearingComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<VirusSharpHearingComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStartup(Entity<VirusSharpHearingComponent> ent, ref ComponentStartup args)
    {
        if (ent.Comp.ForceKeen)
        {
            if (!TryComp<KeenHearingComponent>(ent, out var keen))
                return;

            ent.Comp.OriginalKeenState = keen.State;
            ent.Comp.OriginalKeenToggleTime = keen.ToggleTime;
            ent.Comp.CapturedKeenHearing = true;

            keen.State = IgnoreLightVisionOverlayState.Half;
            keen.ToggleTime = null;
            Dirty(ent.Owner, keen);
        }
        else
        {
            _actions.AddAction(ent.Owner, ref ent.Comp.ActionEntity, ent.Comp.Action);
        }
    }

    private void OnShutdown(Entity<VirusSharpHearingComponent> ent, ref ComponentShutdown args)
    {
        if (Terminating(ent.Owner))
            return;

        _actions.RemoveAction(ent.Owner, ent.Comp.ActionEntity);

        if (ent.Comp.CapturedKeenHearing && TryComp<KeenHearingComponent>(ent, out var keen))
        {
            keen.State = ent.Comp.OriginalKeenState;
            keen.ToggleTime = ent.Comp.OriginalKeenToggleTime;
            Dirty(ent.Owner, keen);
        }
    }
}
