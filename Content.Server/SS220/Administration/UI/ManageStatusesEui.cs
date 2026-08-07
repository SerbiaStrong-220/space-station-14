// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.EUI;
using Content.Shared.Administration;
using Content.Shared.Eui;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Server.Administration.UI;

public sealed class ManageStatusesEui : BaseEui
{
    [Dependency] private IEntityManager _entityManager = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;

    private readonly StatusEffectsSystem _statusEffects;
    private readonly EntityUid _targetEntity;
    private readonly string _targetName;

    public ManageStatusesEui(EntityUid targetEntity, string targetName)
    {
        IoCManager.InjectDependencies(this);
        _statusEffects = _entityManager.System<StatusEffectsSystem>();
        _targetEntity = targetEntity;
        _targetName = targetName;
    }

    public override void Opened()
    {
        base.Opened();
        StateDirty();
    }

    public override EuiStateBase GetNewState()
    {
        var allStatuses = new Dictionary<string, string>();
        var activeStatuses = new List<string>();

        foreach (var protoId in _statusEffects.StatusEffectPrototypes)
        {
            if (_prototypeManager.TryIndex<EntityPrototype>(protoId, out var proto))
            {
                var name = Loc.GetString(proto.Name);
                if (string.IsNullOrWhiteSpace(name))
                    name = protoId;

                allStatuses[protoId] = name;

                if (_statusEffects.HasStatusEffect(_targetEntity, new EntProtoId(protoId)))
                {
                    activeStatuses.Add(protoId);
                }
            }
        }

        return new ManageStatusesEuiState(_targetName, activeStatuses, allStatuses);
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (_entityManager.Deleted(_targetEntity))
        {
            Close();
            return;
        }

        switch (msg)
        {
            case AddStatusMessage addMsg:
                _statusEffects.TrySetStatusEffectDuration(_targetEntity, new EntProtoId(addMsg.StatusId), duration: null, delay: null);
                StateDirty();
                break;

            case RemoveStatusMessage removeMsg:
                _statusEffects.TryRemoveStatusEffect(_targetEntity, new EntProtoId(removeMsg.StatusId));
                StateDirty();
                break;
        }
    }
}
