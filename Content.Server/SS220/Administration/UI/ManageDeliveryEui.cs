// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Delivery;
using Content.Server.EUI;
using Content.Server.StationRecords.Systems;
using Content.Shared.CrewManifest;
using Content.Shared.Delivery;
using Content.Shared.Eui;
using Content.Shared.Popups;
using Content.Shared.SS220.Administration;
using Content.Shared.StationRecords;
using Robust.Shared.Containers;

namespace Content.Server.SS220.Administration.UI;

public sealed partial class ManageDeliveryEui : BaseEui
{
    [Dependency] private IEntityManager _entityManager = default!;

    private readonly DeliverySystem _delivery;
    private readonly DeliveryModifierSystem _modifier;
    private readonly StationRecordsSystem _records;
    private readonly SharedContainerSystem _container;
    private readonly SharedPopupSystem _popup;
    private readonly EntityUid _targetEntity;
    private readonly string _targetName;

    public ManageDeliveryEui(EntityUid targetEntity, string targetName)
    {
        IoCManager.InjectDependencies(this);
        _delivery = _entityManager.System<DeliverySystem>();
        _modifier = _entityManager.System<DeliveryModifierSystem>();
        _records = _entityManager.System<StationRecordsSystem>();
        _container = _entityManager.System<SharedContainerSystem>();
        _popup = _entityManager.System<SharedPopupSystem>();
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
            return new ManageDeliveryEuiState(_targetName);

        var hasTimer = _entityManager.TryGetComponent<DeliveryPriorityComponent>(_targetEntity, out _);
        var contentsProtoId = GetContents(delivery);

        return new ManageDeliveryEuiState(
            targetName: _targetName,
            baseSpesoReward: delivery.BaseSpesoReward,
            hasTimer: hasTimer,
            timeLeftSeconds: hasTimer ? (int)_modifier.GetTimeLeft(_targetEntity).TotalSeconds : 0,
            recipientName: delivery.RecipientName,
            recipientJobTitle: delivery.RecipientJobTitle,
            contentsProtoId: contentsProtoId,
            crew: BuildCrewList(delivery.RecipientStation));
    }

    private string? GetContents(DeliveryComponent delivery)
    {
        if (!_container.TryGetContainer(_targetEntity, delivery.Container, out var container))
            return null;

        foreach (var uid in container.ContainedEntities)
        {
            if (_entityManager.TryGetComponent<MetaDataComponent>(uid, out var meta) &&
                meta.EntityPrototype != null)
            {
                return meta.EntityPrototype.ID;
            }
        }

        return null;
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
            case ReplaceDeliveryContentsMessage contentsMsg:
                if (!_delivery.ReplaceContents(_targetEntity, contentsMsg.ProtoId))
                    ReportContentsFailure(contentsMsg.ProtoId);

                StateDirty();
                break;
            case ClearDeliveryContentsMessage:
                _delivery.ClearContents(_targetEntity);
                StateDirty();
                break;
        }
    }

    private void ReportContentsFailure(string protoId)
    {
        if (Player.AttachedEntity is { } attached)
            _popup.PopupClient(Loc.GetString("admin-verbs-delivery-contents-failed", ("proto", protoId)), attached);
    }
}

