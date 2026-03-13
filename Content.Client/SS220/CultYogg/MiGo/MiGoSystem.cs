// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Client.Movement.Systems;
using Content.Shared.SS220.CultYogg.MiGo;
using Robust.Client.GameObjects;

namespace Content.Client.SS220.CultYogg.MiGo;

public sealed class MiGoSystem : SharedMiGoSystem
{
    [Dependency] private readonly ContentEyeSystem _contentEye = default!;
    [Dependency] private readonly PointLightSystem _pointLightSystem = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MiGoComponent, MiGoToggleLightEvent>(OnMiGoToggleLightAction);
    }

    private void OnMiGoToggleLightAction(Entity<MiGoComponent> ent, ref MiGoToggleLightEvent args)
    {
        if (args.Handled)
            return;

        TryComp<PointLightComponent>(ent, out var light);

        if (!TryComp<EyeComponent>(ent, out var eye))
            return;

        if (!eye.DrawLight)
        {
            // normal lighting
            _contentEye.RequestEye(eye.DrawFov, true);
        }
        else if (light != null)
        {
            // personal lighting
            var newState = !light.Enabled;
            _pointLightSystem.SetEnabled(ent.Owner, newState, light);
        }

        args.Handled = true;
    }
}
