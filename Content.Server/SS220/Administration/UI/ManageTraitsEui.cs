using Content.Server.EUI;
using Content.Server.Traits;
using Content.Shared.Administration;
using Content.Shared.Eui;
using Content.Shared.Traits;
using Robust.Shared.Prototypes;

namespace Content.Server.Administration.UI;

public sealed class ManageTraitsEui : BaseEui
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private readonly TraitSystem _traitSystem;
    private readonly EntityUid _targetEntity;
    private readonly string _targetName;

    public ManageTraitsEui(EntityUid targetEntity, string targetName)
    {
        IoCManager.InjectDependencies(this);
        _traitSystem = _entityManager.System<TraitSystem>();
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
        var allTraits = new Dictionary<string, string>();
        foreach (var trait in _prototypeManager.EnumeratePrototypes<TraitPrototype>())
        {
            allTraits[trait.ID] = Loc.GetString(trait.Name);
        }

        var activeTraits = _traitSystem.GetActiveTraits(_targetEntity);
        return new ManageTraitsEuiState(_targetName, activeTraits, allTraits);
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
            case AddTraitMessage addMsg:
                _traitSystem.AddTrait(_targetEntity, addMsg.TraitId, addMsg.SpawnGear);
                StateDirty();
                break;
            case RemoveTraitMessage removeMsg:
                _traitSystem.RemoveTrait(_targetEntity, removeMsg.TraitId);
                StateDirty();
                break;
        }
    }
}
