// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.SS220.Pathology;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.PathologyProvider;

/// <summary>
/// Organ damage sources that are not reagents. Reagents add organ damage with the AddPathology entity effect.
/// </summary>
public sealed partial class PathologyProviderSystem
{
    private static readonly ProtoId<PathologyPrototype> RetinaDamage = "RetinaDamage";

    private void InitializeOrganSources()
    {
        SubscribeLocalEvent<PathologyHolderComponent, EyeDamageChangedEvent>(OnEyeDamageChanged);
    }

    private void OnEyeDamageChanged(Entity<PathologyHolderComponent> entity, ref EyeDamageChangedEvent args)
    {
        if (!TryComp<BlindableComponent>(entity, out var blindable) || args.Damage < blindable.MaxDamage)
            return;

        // permanent blindness forces eye damage up, it isn't an injury of the eyes
        if (blindable.MinDamage >= blindable.MaxDamage)
            return;

        // only further injuries of already burnt out eyes damage the retina, e.g. welding without protection
        if (_pathology.HavePathology(entity.AsNullable(), RetinaDamage))
            _pathology.TryChangePathologyStack(entity.AsNullable(), RetinaDamage);
        else
            _pathology.TryAddPathology(entity.AsNullable(), RetinaDamage);
    }
}
