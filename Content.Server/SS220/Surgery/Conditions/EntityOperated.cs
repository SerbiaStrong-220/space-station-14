using Content.Server.SS220.Surgery.Components;
using Content.Shared.Construction;
using Content.Shared.Examine;
using JetBrains.Annotations;

namespace Content.Server.Construction.Conditions;
[UsedImplicitly]
[DataDefinition]
public sealed partial class EntityOperated : IGraphCondition
{
    public bool Condition(EntityUid uid, IEntityManager entityManager)
    {
        var aboba = entityManager.GetComponent<OperapableComponent>(uid);
        return aboba.IsOperated;
    }

    public bool DoExamine(ExaminedEvent args)
    {
        var entity = args.Examined;
        var operated = IoCManager.Resolve<IEntityManager>().GetComponent<OperapableComponent>(entity).IsOperated;

        return false;
    }
    public IEnumerable<ConstructionGuideEntry> GenerateGuideEntry()
    {
        yield return new ConstructionGuideEntry()
        {

        };
    }
}
