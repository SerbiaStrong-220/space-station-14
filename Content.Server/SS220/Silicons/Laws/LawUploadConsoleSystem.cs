// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using Content.Server.Silicons.Laws;
using Content.Server.Station.Systems;
using Content.Shared.Access.Components;
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
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
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
        return GetSlotItem(uid, LawUploadConsoleComponent.CardSlot) is { } card &&
               HasComp<IdCardComponent>(card) && _access.IsAllowed(card, uid);
    }

    private void OnApply(Entity<LawUploadConsoleComponent> ent, ref ApplyStationLawsMessage args)
    {
        // Never trust the buttons' enabled state, or a preview from before the board/card was replaced.
        if (!Enum.IsDefined(args.Target) || args.Revision != ent.Comp.Revision ||
            !_ui.IsUiOpen(ent.Owner, LawUploadUiKey.Key, args.Actor) ||
            !_power.IsPowered(ent.Owner) || !HasAuthorizedCard(ent) ||
            _station.GetOwningStation(ent.Owner) is not { } station ||
            GetSlotItem(ent, LawUploadConsoleComponent.BoardSlot) is not { } board ||
            !TryComp<SiliconLawProviderComponent>(board, out var provider))
        {
            UpdateUi(ent);
            return;
        }

        var lawset = provider.Lawset ?? _laws.GetLawset(provider.Laws);
        var count = _laws.UploadStationLawset(station, args.Target, provider.Laws, lawset, provider.LawUploadSound);
        _adminLog.Add(LogType.Action, LogImpact.High,
            $"{ToPrettyString(args.Actor):player} uploaded lawset {provider.Laws} from {ToPrettyString(board)} " +
            $"using {ToPrettyString(ent.Owner)} to {args.Target} on {ToPrettyString(station)} ({count} recipients). Laws: {lawset.LoggingString()}");

        // The station event refreshes every console and invalidates duplicate confirmations.
    }

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

    private string LawsetName(ProtoId<SiliconLawsetPrototype> id)
    {
        var prototype = _prototypes.Index(id);
        return prototype.Name is { } name ? Loc.GetString(name) : id.Id;
    }

    private string[] LawLines(SiliconLawset lawset) => lawset.Laws.Select(law =>
        $"{law.LawIdentifierOverride ?? law.Order.ToString()}. {Loc.GetString(law.LawString)}").ToArray();

    public void UpdateUi(Entity<LawUploadConsoleComponent> ent)
    {
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

        var aiName = string.Empty;
        var borgName = string.Empty;
        string[] aiLaws = [];
        string[] borgLaws = [];
        if (station is { } stationUid)
        {
            var ai = _laws.GetStationLawset(stationUid, LawUploadTarget.Ai);
            var borg = _laws.GetStationLawset(stationUid, LawUploadTarget.Borgs);
            aiName = LawsetName(ai.Id);
            aiLaws = LawLines(ai.Laws);
            borgName = LawsetName(borg.Id);
            borgLaws = LawLines(borg.Laws);
        }

        _ui.SetUiState(ent.Owner, LawUploadUiKey.Key, new LawUploadState(
            ent.Comp.Revision, status, card != null, board != null,
            powered && station != null && authorized && validBoard,
            aiName, aiLaws, borgName, borgLaws,
            validBoard ? LawsetName(provider!.Laws) : string.Empty,
            validBoard ? LawLines(provider!.Lawset ?? _laws.GetLawset(provider.Laws)) : []));
    }
}
