using Content.Shared.Bed.Cryostorage;
using Content.Shared.Ghost;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.SS220.GhostExtension;
using Content.Shared.SS220.Mind;
using Robust.Shared.Network;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.SS220.MindExtension;
public abstract partial class SharedMindExtensionSystem : EntitySystem
{
    [Dependency] protected readonly EntityManager _entityManager = default!;

    private EntityQuery<MindExtensionComponent> _mindExtQuery;

    #region Helpers
    public override void Initialize()
    {
        base.Initialize();

        _mindExtQuery = GetEntityQuery<MindExtensionComponent>();
    }
    public bool TryGetMindExtension(NetUserId player, [NotNullWhen(true)] out Entity<MindExtensionComponent>? entity)
    {
        var mindExts = _entityManager.AllComponents<MindExtensionComponent>();
        entity = mindExts.FirstOrNull(x => x.Component.PlayerSession == player);

        return entity is not null;
    }

    public bool TryGetMindExtension(MindExtensionContainerComponent container, [NotNullWhen(true)] out Entity<MindExtensionComponent>? entity)
    {
        entity = null;

        if (container.MindExtension is null)
            return false;

        entity = _mindExtQuery.Get((EntityUid)container.MindExtension);

        return entity is not null;
    }

    protected bool CheckEntityAbandoned(EntityUid entity)
    {
        //Если ливаем не из тела, то норм.
        if (!TryComp<MobStateComponent>(entity, out var mobState))
            return false;

        //Если ливаем из мертвого тела, то норм.
        switch (mobState.CurrentState)
        {
            case Shared.Mobs.MobState.Invalid:
                return false;
            case Shared.Mobs.MobState.Dead:
                return false;
        }

        //Если ливаем из живого тела, то пока пиздося с гарантом, кроме случая суицида.
        return true;
    }

    protected void ChangeOrAddTrailPoint(MindExtensionComponent comp, EntityUid entity, bool isAbandoned)
    {
        if (!IsAvaibleToRememberEntity(entity))
            return;

        if (comp.Trail.ContainsKey(entity))
        {
            comp.Trail[entity].IsAbandoned = isAbandoned;
            return;
        }

        TryComp(entity, out MetaDataComponent? metaData);

        comp.Trail.Add(entity, new TrailPointMetaData()
        {
            EntityName = metaData?.EntityName ?? "",
            EntityDescription = metaData?.EntityDescription ?? "",
            IsAbandoned = isAbandoned
        });
    }

    protected (EntityUid Uid, MindExtensionComponent Component) CreateExtensionEntity(NetUserId playerSession)
    {
        var newEnt = _entityManager.CreateEntityUninitialized(null);
        var mindExtComponent = new MindExtensionComponent() { PlayerSession = playerSession };

        _entityManager.AddComponent(newEnt, mindExtComponent);
        _entityManager.InitializeEntity(newEnt);
        return new(newEnt, mindExtComponent);
    }
    #endregion

    #region Validators
    protected bool IsAvaibleToRememberEntity(EntityUid? entity)
    {
        if (HasComp<GhostComponent>(entity))
            return false;

        return true;
    }

    protected BodyStateToEnter IsAvaibleToEnterEntity(EntityUid target, MindExtensionComponent mindExtension, NetUserId session)
    {
        if (!_entityManager.EntityExists(target))
            return BodyStateToEnter.Destroyed;

        if (TryComp<CryostorageContainedComponent>(target, out var cryo))
            return BodyStateToEnter.InCryo;

        //При Visit MindConatainer может остаться, как и Mind. Нужно проверить, не является-ли этот Mind своим.
        //Если Mind не свой, значит тело занято.
        if (TryComp<MindContainerComponent>(target, out var mindContainer) && mindContainer.Mind is not null)
            if (TryComp<MindComponent>(mindContainer.Mind, out var mind) && mind.UserId != session)
                return BodyStateToEnter.Engaged;

        if (mindExtension.Trail.TryGetValue(target, out var metaData))
        {
            if (metaData.IsAbandoned)
                return BodyStateToEnter.Abandoned;
        }

        return BodyStateToEnter.Avaible;
    }


    #endregion

}
/*
[ByRefEvent]
public record struct MindSwappedEvent(EntityUid Entity, EntityUid Mind);*/
