// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Virology.Behaviors;
using Robust.Server.GameObjects;

namespace Content.Server.SS220.Virology.Behaviors;

public static class VirusGlow
{
    public static void Apply(IEntityManager entMan, SharedPointLightSystem light, EntityUid uid, Color color, float radius, float energy, ref VirusGlowState state)
    {
        if (entMan.TryGetComponent<PointLightComponent>(uid, out var existing))
        {
            state.Added = false;
            state.SavedColor = existing.Color;
            state.SavedRadius = existing.Radius;
            state.SavedEnergy = existing.Energy;
            state.SavedEnabled = existing.Enabled;
        }
        else
        {
            state.Added = true;
        }

        entMan.EnsureComponent<PointLightComponent>(uid);
        light.SetColor(uid, color);
        light.SetRadius(uid, radius);
        light.SetEnergy(uid, energy);
    }

    public static void Restore(IEntityManager entMan, SharedPointLightSystem light, EntityUid uid, VirusGlowState state)
    {
        if (state.Added)
        {
            entMan.RemoveComponent<PointLightComponent>(uid);
            return;
        }

        if (!entMan.HasComponent<PointLightComponent>(uid))
            return;

        // restore original
        light.SetColor(uid, state.SavedColor);
        light.SetRadius(uid, state.SavedRadius);
        light.SetEnergy(uid, state.SavedEnergy);
        light.SetEnabled(uid, state.SavedEnabled);
    }
}
