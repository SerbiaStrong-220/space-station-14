// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Experience;
using Content.Shared.SS220.Experience.Systems;
using Content.Shared.SS220.HiddenDescription;
using Robust.Client.Player;
using Robust.Shared.Player;

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
        SubscribeLocalEvent<HiddenDescriptionComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<HiddenDescriptionComponent, ChangeHiddenDescriptions>(OnChangeHiddenDescriptions);

        SubscribeLocalEvent<ExperienceComponent, KnowledgeGainedEvent>(OnKnowledgeGained);
    }

    private void OnComponentInit(Entity<HiddenDescriptionComponent> entity, ref ComponentInit _)
    {
        ChangeDescription(entity);
    }

    private void OnPlayerAttached(Entity<HiddenDescriptionComponent> entity, ref PlayerAttachedEvent _)
    {
        ChangeDescription(entity);
    }

    private void OnChangeHiddenDescriptions(Entity<HiddenDescriptionComponent> entity, ref ChangeHiddenDescriptions _)
    {
        ChangeDescription(entity);
    }

    private void OnKnowledgeGained(Entity<ExperienceComponent> entity, ref KnowledgeGainedEvent _)
    {
        if (_playerManager.LocalEntity is not { } localEntity)
            return;

        if (_playerManager.LocalEntity != entity)
            return;

        var ev = new ChangeHiddenDescriptions();
        RaiseLocalEvent(ref ev);
    }

    private void ChangeDescription(Entity<HiddenDescriptionComponent> entity)
    {
        if (entity.Comp.HiddenName is null || entity.Comp.NameEntries.Count == 0)
            return;

        if (_playerManager.LocalEntity is not { Valid: true } localEntity)
            return;

        if (!TryComp<ExperienceComponent>(localEntity, out var experienceComponent) || HasComp<BypassKnowledgeCheckComponent>(localEntity))
            return;

        // we return because entries in list goes in order of showing
        foreach (var (knowledgeId, locId) in entity.Comp.NameEntries)
        {
            if (_experience.HaveKnowledge((localEntity, experienceComponent), knowledgeId))
            {
                if (locId is not null)
                    _metaData.SetEntityName(entity, Loc.GetString(locId));

                return;
            }
        }

        _metaData.SetEntityName(entity, Loc.GetString(entity.Comp.HiddenName));
    }
}
