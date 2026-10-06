// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using Content.Server.Silicons.Laws;
using Content.Server.Station.Systems;
using Content.Shared.Access.Systems;
using Content.Shared.Administration.Logs;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.SS220.Silicons.Laws;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Silicons.Laws;

public sealed class LawUploadConsoleSystem : EntitySystem
{
    [Dependency] private readonly SiliconLawSystem _laws = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly SharedContainerSystem _containers = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _power = default!;
    [Dependency] private readonly AccessReaderSystem _access = default!;
    // SS220 random lawset begin
    [Dependency] private readonly SharedIdCardSystem _idCard = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    // SS220 random lawset end
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<LawUploadConsoleComponent, BoundUIOpenedEvent>(OnOpen);
        SubscribeLocalEvent<LawUploadConsoleComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<LawUploadConsoleComponent, EntRemovedFromContainerMessage>(OnRemoved);
        SubscribeLocalEvent<LawUploadConsoleComponent, PowerChangedEvent>(OnPower);
        SubscribeLocalEvent<LawUploadConsoleComponent, InteractUsingEvent>(OnInteract,
            after: new[] { typeof(ItemSlotsSystem) });
        SubscribeLocalEvent<LawUploadConsoleComponent, ApplyStationLawsMessage>(OnApply);
        SubscribeLocalEvent<StationLawsetsChangedEvent>(OnStationChanged);
    }

    private void OnOpen(Entity<LawUploadConsoleComponent> ent, ref BoundUIOpenedEvent args) => UpdateUi(ent);

    private void OnInserted(Entity<LawUploadConsoleComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (IsUploadSlot(args.Container.ID))
            Invalidate(ent);
    }

    private void OnRemoved(Entity<LawUploadConsoleComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (IsUploadSlot(args.Container.ID))
            Invalidate(ent);
    }

    private static bool IsUploadSlot(string id) =>
        id is LawUploadConsoleComponent.CardSlot or LawUploadConsoleComponent.BoardSlot;

    private void OnPower(Entity<LawUploadConsoleComponent> ent, ref PowerChangedEvent args) => Invalidate(ent);

    private void OnInteract(Entity<LawUploadConsoleComponent> ent, ref InteractUsingEvent args)
    {
        // ItemSlotsSystem has already finished the insertion, including dropping the held item.
        if (!_power.IsPowered(ent.Owner) ||
            (GetSlotItem(ent, LawUploadConsoleComponent.CardSlot) != args.Used &&
             GetSlotItem(ent, LawUploadConsoleComponent.BoardSlot) != args.Used))
            return;

        _ui.TryOpenUi(ent.Owner, LawUploadUiKey.Key, args.User);
    }

    private EntityUid? GetSlotItem(EntityUid uid, string slot)
    {
        if (!_containers.TryGetContainer(uid, slot, out var container) || container.ContainedEntities.Count == 0)
            return null;

        return container.ContainedEntities[0];
    }

    private bool HasAuthorizedCard(EntityUid uid)
    {
        // SS220 random lawset begin
        return GetSlotItem(uid, LawUploadConsoleComponent.CardSlot) is { } card &&
               _idCard.TryGetIdCard(card, out var idCard) &&
               _access.IsAllowed(idCard.Owner, uid);
        // SS220 random lawset end
    }

    private void OnApply(Entity<LawUploadConsoleComponent> ent, ref ApplyStationLawsMessage args)
    {
        // SS220 random lawset begin
        if (!TryGetApplyData(ent, args, out var station, out var board, out var provider))
        {
            UpdateUi(ent);
            return;
        }

        var lawset = provider.Lawset ?? _laws.GetLawset(provider.Laws);
        var count = _laws.UploadStationLawset(station, args.Target, provider.Laws, lawset, provider.LawUploadSound);
        _adminLog.Add(LogType.Action, LogImpact.High,
            $"{ToPrettyString(args.Actor):player} uploaded lawset {provider.Laws} from {ToPrettyString(board)} " +
            $"using {ToPrettyString(ent.Owner)} to {args.Target} on {ToPrettyString(station)} ({count} recipients). Laws: {lawset.LoggingString()}");
        // SS220 random lawset end
    }

    // SS220 random lawset begin
    private bool TryGetApplyData(Entity<LawUploadConsoleComponent> ent, ApplyStationLawsMessage args,
        out EntityUid station, out EntityUid board, out SiliconLawProviderComponent provider)
    {
        station = default;
        board = default;
        provider = default!;

        if (!Enum.IsDefined(args.Target) || args.Revision != ent.Comp.Revision)
            return false;

        if (!_ui.IsUiOpen(ent.Owner, LawUploadUiKey.Key, args.Actor))
            return false;

        if (!_power.IsPowered(ent.Owner) || !HasAuthorizedCard(ent))
            return false;

        if (_station.GetOwningStation(ent.Owner) is not { } stationUid)
            return false;

        if (GetSlotItem(ent, LawUploadConsoleComponent.BoardSlot) is not { } boardUid ||
            !TryComp(boardUid, out SiliconLawProviderComponent? providerComp))
            return false;

        station = stationUid;
        board = boardUid;
        provider = providerComp;
        return true;
    }
    // SS220 random lawset end

    private void OnStationChanged(StationLawsetsChangedEvent args)
    {
        var query = EntityQueryEnumerator<LawUploadConsoleComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_station.GetOwningStation(uid) == args.Station)
                Invalidate((uid, comp));
        }
    }

    private void Invalidate(Entity<LawUploadConsoleComponent> ent)
    {
        ent.Comp.Revision++;
        UpdateUi(ent);
    }

    public void UpdateUi(Entity<LawUploadConsoleComponent> ent)
    {
        // SS220 random lawset begin
        var card = GetSlotItem(ent, LawUploadConsoleComponent.CardSlot);
        var board = GetSlotItem(ent, LawUploadConsoleComponent.BoardSlot);
        var station = _station.GetOwningStation(ent.Owner);
        var powered = _power.IsPowered(ent.Owner);
        var authorized = HasAuthorizedCard(ent);
        var validBoard = TryComp<SiliconLawProviderComponent>(board, out var provider);

        var status = !powered ? "law-upload-no-power" :
            station == null ? "law-upload-no-station" :
            card == null ? "law-upload-insert-card" :
            !authorized ? "law-upload-access-denied" :
            !validBoard ? "law-upload-insert-board" : "law-upload-ready";

        ProtoId<SiliconLawsetPrototype> aiLawset = default;
        ProtoId<SiliconLawsetPrototype> borgLawset = default;
        ProtoId<SiliconLawPrototype>[] aiLaws = [];
        ProtoId<SiliconLawPrototype>[] borgLaws = [];
        if (station is { } stationUid)
        {
            var ai = _laws.GetStationLawset(stationUid, LawUploadTarget.Ai);
            var borg = _laws.GetStationLawset(stationUid, LawUploadTarget.Borgs);
            aiLawset = ai.Id;
            aiLaws = LawIds(ai.Id);
            borgLawset = borg.Id;
            borgLaws = LawIds(borg.Id);
        }

        var boardLawset = validBoard ? provider!.Laws : default;
        _ui.SetUiState(ent.Owner, LawUploadUiKey.Key, new LawUploadState(
            ent.Comp.Revision, status, card != null, board != null,
            powered && station != null && authorized && validBoard,
            aiLawset, aiLaws, borgLawset, borgLaws, boardLawset, LawIds(boardLawset)));
        // SS220 random lawset end
    }

    // SS220 random lawset begin
    private ProtoId<SiliconLawPrototype>[] LawIds(ProtoId<SiliconLawsetPrototype> lawset)
    {
        if (string.IsNullOrEmpty(lawset.Id) ||
            !_prototypes.TryIndex(lawset, out var prototype))
            return [];

        return prototype.Laws.ToArray();
    }
    // SS220 random lawset end
}
