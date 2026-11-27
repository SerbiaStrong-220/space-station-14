using System.Linq;
using Content.Shared.SS220.DarkForces.Narsi.Abilities.Events;
using Content.Shared.SS220.DarkForces.Narsi.Roles;
using Robust.Shared.Random;
using NarsiTeleportRuneComponent = Content.Server.SS220.DarkForces.Narsi.Runes.Components.NarsiTeleportRuneComponent;

namespace Content.Server.SS220.DarkForces.Narsi.Cultist.Abilities;

public sealed partial class NarsiCultistAbilitiesSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;

    private void InitializeTeleport()
    {
        SubscribeLocalEvent<NarsiCultistComponent, NarsiCultistTeleportEvent>(OnTeleportEvent);
    }

    private void OnTeleportEvent(EntityUid uid, NarsiCultistComponent component, NarsiCultistTeleportEvent args)
    {
        if (args.Handled)
            return;

        var teleportRunes = EntityQuery<NarsiTeleportRuneComponent, TransformComponent>().ToList();
        if (!teleportRunes.Any())
            return;

        var targetRune = _random.Pick(teleportRunes);

        _transformSystem.SetCoordinates(uid, targetRune.Item2.Coordinates);
        _transformSystem.AttachToGridOrMap(uid);
        OnCultistAbility(uid, args);

        args.Handled = true;
    }
}
