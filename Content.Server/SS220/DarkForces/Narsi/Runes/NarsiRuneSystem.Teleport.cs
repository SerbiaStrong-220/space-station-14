using System.Linq;
using Content.Server.Administration;
using Robust.Shared.Player;
using NarsiTeleportRuneComponent = Content.Server.SS220.DarkForces.Narsi.Runes.Components.NarsiTeleportRuneComponent;

namespace Content.Server.SS220.DarkForces.Narsi.Runes;

public sealed partial class NarsiRuneSystem
{
    [Dependency] private readonly QuickDialogSystem _quickDialog = default!;

    private void ShowPopupTag(EntityUid user, NarsiTeleportRuneComponent runeComponent)
    {
        if (!TryComp<ActorComponent>(user, out var actorComponent))
            return;

        _quickDialog.OpenDialog(actorComponent.PlayerSession, "Укажите тег для руны телепорта", "Тег", (string message) =>
        {
            runeComponent.Tag = message;
        });
    }

    private void TeleportRuneVerb(EntityUid rune, string tag)
    {
        EntityUid? selectedRune = null;
        var runes = EntityQueryEnumerator<NarsiTeleportRuneComponent>();
        while (runes.MoveNext(out var fRune, out var runeComponent))
        {
            if (fRune == rune || runeComponent.Tag != tag)
                continue;

            selectedRune = fRune;
            break;
        }

        if (selectedRune == null)
        {
            _popupSystem.PopupEntity("Не найдена руна с таким же тегом...", rune);
            return;
        }

        var targetCoords = Transform(selectedRune.Value).Coordinates;

        var entitiesInRange = FindHumanoidsNearRune(rune, 1.0f);
        if (!entitiesInRange.Any())
            return;

        foreach (var entity in entitiesInRange)
        {
            _transformSystem.SetCoordinates(entity, targetCoords);
            _transformSystem.AttachToGridOrMap(entity);
        }
    }
}
