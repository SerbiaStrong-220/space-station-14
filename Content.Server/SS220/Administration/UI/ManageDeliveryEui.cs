// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Delivery;
using Content.Server.EUI;
using Content.Server.StationRecords.Systems;
using Content.Shared.Administration;
using Content.Shared.CrewManifest;
using Content.Shared.Delivery;
using Content.Shared.Eui;
using Content.Shared.StationRecords;

namespace Content.Server.Administration.UI;

public sealed partial class ManageDeliveryEui : BaseEui
{
    [Dependency] private IEntityManager _entityManager = default!;

    private readonly DeliverySystem _delivery;
    private readonly DeliveryModifierSystem _modifier;
    private readonly StationRecordsSystem _records;
    private readonly EntityUid _targetEntity;
    private readonly string _targetName;

    public ManageDeliveryEui(EntityUid targetEntity, string targetName)
    {
        IoCManager.InjectDependencies(this);
        _delivery = _entityManager.System<DeliverySystem>();
        _modifier = _entityManager.System<DeliveryModifierSystem>();
        _records = _entityManager.System<StationRecordsSystem>();
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
        if (!_entityManager.TryGetComponent<DeliveryComponent>(_targetEntity, out var delivery))
            return new ManageDeliveryEuiState { TargetName = _targetName };

        var hasTimer = _entityManager.TryGetComponent<DeliveryPriorityComponent>(_targetEntity, out _);

        return new ManageDeliveryEuiState
        {
            TargetName = _targetName,
            BaseSpesoReward = delivery.BaseSpesoReward,
            HasTimer = hasTimer,
            TimeLeftSeconds = hasTimer ? (int)_modifier.GetTimeLeft(_targetEntity).TotalSeconds : 0,
            RecipientName = delivery.RecipientName,
            RecipientJobTitle = delivery.RecipientJobTitle,
            Crew = BuildCrewList(delivery.RecipientStation),
        };
    }

    private List<(uint, CrewManifestEntry)> BuildCrewList(EntityUid? station)
    {
        var crew = new List<(uint, CrewManifestEntry)>();

        if (station is not { } stationId)
            return crew;

        foreach (var (id, record) in _records.GetRecordsOfType<GeneralStationRecord>(stationId))
        {
            crew.Add((id, new CrewManifestEntry(
                record.Name,
                record.JobTitle,
                record.JobIcon,
                record.JobPrototype,
                record.IsInCryo)));
        }

        crew.Sort((x, y) => string.Compare(x.Item2.Name, y.Item2.Name, StringComparison.CurrentCultureIgnoreCase));
        return crew;
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
            case SetDeliveryRewardMessage rewardMsg:
                _delivery.SetBaseSpesoReward(_targetEntity, rewardMsg.Reward);
                StateDirty();
                break;
            case SetDeliveryRecipientMessage recipientMsg:
                _delivery.SetRecipient(_targetEntity, recipientMsg.RecordId);
                StateDirty();
                break;
            case SetDeliveryTimerMessage timerMsg:
                _modifier.SetTimeLeft(_targetEntity, TimeSpan.FromSeconds(Math.Max(0, timerMsg.Seconds)));
                StateDirty();
                break;
        }
    }
}

