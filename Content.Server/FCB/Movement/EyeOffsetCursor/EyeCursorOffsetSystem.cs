// © FCB, MIT, full text: https://github.com/Free-code-base-14/space-station-14/blob/master/LICENSE.TXT
using Content.Server.Movement.Components;
using Content.Server.Movement.Systems;
using Content.Shared.Camera;
using Content.Shared.FCB.ComplexRepairable;

namespace Content.Server.FCB.Movement.EyeCursorOffset;

public sealed class EyeCursorOffsetSystem : EntitySystem
{
    [Dependency] private readonly ContentEyeSystem _eye = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EyeOffsetInCombatModeComponent, ComponentStartup>(OnOffsetStartup);
        SubscribeLocalEvent<EyeCursorOffsetComponent, GetEyePvsScaleEvent>(OnGetEyePvsScale);
    }

    private void OnOffsetStartup(Entity<EyeOffsetInCombatModeComponent> ent, ref ComponentStartup args)
    {
        _eye.UpdatePvsScale(ent.Owner);
    }

    private void OnGetEyePvsScale(Entity<EyeCursorOffsetComponent> ent, ref GetEyePvsScaleEvent args)
    {
        args.Scale += ent.Comp.PvsIncrease;
    }
}
