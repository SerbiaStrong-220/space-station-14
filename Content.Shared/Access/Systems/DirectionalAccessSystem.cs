using System.Linq;
using System.Text;
using Content.Shared.Access.Components;

namespace Content.Shared.Access.Systems;

public sealed class DirectionalAccessSystem : EntitySystem
{
    /// <summary>
    /// Gets absolute positions for an entity that requires access and target entity
    /// Calculates the relative cardinal direction (N, E, S, W)
    /// then compares it with the readers directions list to see if it is allowed.
    /// </summary>
    /// <param name="targetUid">The entity that targeted for an access.</param>
    /// <param name="requesterUid">The entity that wants an access.</param>
    /// <param name="reader">A reader from a targeted entity</param>
    public bool IsDirectionAllowed(EntityUid targetUid, EntityUid requesterUid, DirectionalAccessComponent reader)
    {
        var accessDirections = new List<Components.Directions>();
        TryComp<TransformComponent>(targetUid, out var targetTransformComponent);
        TryComp<TransformComponent>(requesterUid, out var requesterTransformComponent);

        if (targetTransformComponent == null || requesterTransformComponent == null)
            return false;

        // target.X > requester.X means that a requester positioned to the west (left by default) to a target
        accessDirections.Add(targetTransformComponent!.Coordinates.X > requesterTransformComponent!.Coordinates.X
            ? Components.Directions.West
            : Components.Directions.East);
        // target.Y > requester.Y means that a requester positioned to the south (below by default) to a target
        accessDirections.Add(targetTransformComponent!.Coordinates.Y > requesterTransformComponent!.Coordinates.Y
            ? Components.Directions.South
            : Components.Directions.North);

       return reader.DirectionsList.Any(x => accessDirections.Any(y => y == x));
    }
}
