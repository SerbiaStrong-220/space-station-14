using Content.Server.Movement.Components;
using Content.Server.Movement.Systems;
using Content.Shared.Camera;
using Content.Shared.Hands;
using Content.Shared.Movement.Components;


namespace Content.Server.FCB.Movement.EyeOffsetCursor;

public sealed class EyeOffsetCursorSystem : EntitySystem// I do not know WHY WizDen didn't make this themselves but a fact is a fact - EyeOffsetCursor's PVS increase simply didn't work without the wielding comp lol
{
    [Dependency] private readonly ContentEyeSystem _eye = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EyeCursorOffsetComponent, ComponentStartup>(OnOffsetStartup);
        SubscribeLocalEvent<EyeCursorOffsetComponent, GetEyePvsScaleEvent>(OnGetEyePvsScale);
    }

    private void OnOffsetStartup(Entity<EyeCursorOffsetComponent> ent, ref ComponentStartup args)
    {
        //_eye.UpdatePvsScale(ent.Owner);
    }

    private void OnGetEyePvsScale(Entity<EyeCursorOffsetComponent> ent, ref GetEyePvsScaleEvent args)
    {
        args.Scale += ent.Comp.PvsIncrease;
    }
}
