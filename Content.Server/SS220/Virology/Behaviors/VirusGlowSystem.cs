// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Virology.Behaviors;
using Robust.Server.GameObjects;

namespace Content.Server.SS220.Virology.Behaviors;

public sealed partial class VirusGlowSystem : EntitySystem
{
    [Dependency] private SharedPointLightSystem _light = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VirusGlowComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<VirusGlowComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStartup(Entity<VirusGlowComponent> ent, ref ComponentStartup args)
    {
        if (TryComp<PointLightComponent>(ent, out var existing))
        {
            ent.Comp.Added = false;
            ent.Comp.SavedColor = existing.Color;
            ent.Comp.SavedRadius = existing.Radius;
            ent.Comp.SavedEnergy = existing.Energy;
            ent.Comp.SavedEnabled = existing.Enabled;
        }
        else
        {
            ent.Comp.Added = true;
        }

        EnsureComp<PointLightComponent>(ent);
        _light.SetColor(ent, ent.Comp.LightColor);
        _light.SetRadius(ent, ent.Comp.LightRadius);
        _light.SetEnergy(ent, ent.Comp.LightEnergy);
    }

    private void OnShutdown(Entity<VirusGlowComponent> ent, ref ComponentShutdown args)
    {
        if (Terminating(ent.Owner))
            return;

        if (ent.Comp.Added)
        {
            RemComp<PointLightComponent>(ent);
            return;
        }

        if (!HasComp<PointLightComponent>(ent))
            return;

        // restore host's own light
        _light.SetColor(ent, ent.Comp.SavedColor);
        _light.SetRadius(ent, ent.Comp.SavedRadius);
        _light.SetEnergy(ent, ent.Comp.SavedEnergy);
        _light.SetEnabled(ent, ent.Comp.SavedEnabled);
    }
}
