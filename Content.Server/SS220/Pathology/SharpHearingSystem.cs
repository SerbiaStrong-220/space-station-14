// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Actions;
using Content.Shared.SS220.IgnoreLightVision.Components;
using Content.Shared.SS220.Pathology;

namespace Content.Server.SS220.Pathology;

public sealed partial class SharpHearingSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedPathologySystem _pathology = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SharpHearingComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<SharpHearingComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<SharpHearingComponent, PathologySeverityChanged>(OnSeverityChanged);
    }

    private void OnStartup(Entity<SharpHearingComponent> ent, ref ComponentStartup args)
    {
        Apply(ent);
    }

    private void OnSeverityChanged(Entity<SharpHearingComponent> ent, ref PathologySeverityChanged args)
    {
        if (args.PathologyId == ent.Comp.Pathology)
            Apply(ent);
    }

    private void OnShutdown(Entity<SharpHearingComponent> ent, ref ComponentShutdown args)
    {
        if (Terminating(ent.Owner))
            return;

        _actions.RemoveAction(ent.Owner, ent.Comp.ActionEntity);

        // restore pre-existing keen hearing
        if (ent.Comp.CapturedKeenHearing && TryComp<KeenHearingComponent>(ent, out var keen))
        {
            keen.State = ent.Comp.OriginalKeenState;
            keen.ToggleTime = ent.Comp.OriginalKeenToggleTime;
            Dirty(ent.Owner, keen);
        }
    }

    private void Apply(Entity<SharpHearingComponent> ent)
    {
        if (GetStage(ent) >= 1)
        {
            _actions.RemoveAction(ent.Owner, ent.Comp.ActionEntity);
            ent.Comp.ActionEntity = null;

            if (TryComp<KeenHearingComponent>(ent, out var keen))
            {
                if (!ent.Comp.CapturedKeenHearing)
                {
                    ent.Comp.OriginalKeenState = keen.State;
                    ent.Comp.OriginalKeenToggleTime = keen.ToggleTime;
                    ent.Comp.CapturedKeenHearing = true;
                }

                keen.State = IgnoreLightVisionOverlayState.Half;
                keen.ToggleTime = null;
                Dirty(ent.Owner, keen);
            }
        }
        else
        {
            _actions.AddAction(ent.Owner, ref ent.Comp.ActionEntity, ent.Comp.Action);
        }
    }

    private int GetStage(Entity<SharpHearingComponent> ent)
    {
        if (_pathology.TryGetSymptomData(ent.Owner, ent.Comp.Pathology, out var data))
            return data.Level;

        return 0;
    }
}
