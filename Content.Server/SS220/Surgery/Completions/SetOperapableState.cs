using Content.Server.SS220.Surgery.Components;
using Content.Shared.Construction;
using JetBrains.Annotations;

namespace Content.Server.SS220.Surgery.Completions;

public sealed class ConstructionBeforeDeleteEvent : CancellableEntityEventArgs
{
    public EntityUid? User;

    public ConstructionBeforeDeleteEvent(EntityUid? user)
    {
        User = user;
    }
}

[UsedImplicitly]
[DataDefinition]
public sealed partial class SetOperapableState : IGraphAction
{
    public void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager)
    {
        var aboba = entityManager.GetComponent<OperapableComponent>(uid);

        aboba.IsOpened ^= true;
    }
}
