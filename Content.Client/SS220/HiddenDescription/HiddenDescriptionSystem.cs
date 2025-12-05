using Content.Shared.SS220.Experience;
using Content.Shared.SS220.Experience.Systems;
using Content.Shared.SS220.HiddenDescription;
using Robust.Client.Player;

namespace Content.Client.SS220.HiddenDescription;

public sealed class HiddenDescriptionSystem : SharedHiddenDescriptionSystem
{
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly ExperienceSystem _experience = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HiddenDescriptionComponent, ComponentInit>(OnComponentInit);
    }

    /// <summary>
    /// TODO: on changing local entity redo this!
    /// </summary>
    private void OnComponentInit(Entity<HiddenDescriptionComponent> entity, ref ComponentInit _)
    {
        if (entity.Comp.HiddenName is null)
            return;

        var localEntity = _playerManager.LocalEntity;
        if (!TryComp<ExperienceComponent>(localEntity, out var experienceComponent) || HasComp<BypassKnowledgeCheckComponent>(localEntity))
            return;

        if (entity.Comp.NameEntries.Count == 0)
            return;

        // we return because entries in list goes in order of showing
        foreach (var (knowledgeId, locId) in entity.Comp.NameEntries)
        {
            if (_experience.HaveKnowledge((localEntity.Value, experienceComponent), knowledgeId))
            {
                if (locId is not null)
                    _metaData.SetEntityName(entity, Loc.GetString(locId));

                return;
            }
        }

        _metaData.SetEntityName(entity, entity.Comp.HiddenName);
    }
}
