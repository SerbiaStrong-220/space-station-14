// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using Content.Server.Chat.Managers;
using Content.Shared.Chat;
using Content.Shared.GameTicking;
using Content.Shared.Mind;
using Content.Shared.Overlays;
using Content.Shared.Radio.Components;
using Content.Shared.Random.Helpers;
using Content.Shared.Roles;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.Silicons.StationAi;
using Content.Shared.SS220.Silicons.Laws;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Silicons.Laws;

public sealed partial class SiliconLawSystem
{
    // SS220 random lawset begin
    [Dependency] private IRobustRandom _random = default!;

    private readonly Dictionary<EntityUid, (ProtoId<SiliconLawsetPrototype> Id, SiliconLawset Laws)> _stationLawsetCache = new();
    private readonly Dictionary<(EntityUid Station, LawUploadTarget Target),
        (ProtoId<SiliconLawsetPrototype> Id, SiliconLawset Laws)> _stationLawsetOverrides = new();
    private readonly Dictionary<ProtoId<SiliconLawsetPrototype>, float> _randomLawsetWeights = new();

    private static readonly ProtoId<SiliconLawsetPrototype> DefaultCrewLawset = "Crewsimov";

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _stationLawsetCache.Clear();
        _stationLawsetOverrides.Clear();
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (!args.WasModified<SiliconLawsetPrototype>())
            return;

        CacheRandomLawsetWeights();
    }

    private void CacheRandomLawsetWeights()
    {
        _randomLawsetWeights.Clear();

        foreach (var prototype in _prototype.EnumeratePrototypes<SiliconLawsetPrototype>())
        {
            if (!prototype.Randomizable || prototype.Weight.Value <= 0 ||
                !float.IsFinite(prototype.Weight.Value))
                continue;

            _randomLawsetWeights.Add(prototype, prototype.Weight.Value);
        }
    }

    private void OnLawProviderMapInit(Entity<SiliconLawProviderComponent> entity, ref MapInitEvent args)
    {
        InitializeRandomLawset(entity);
    }

    private void InitializeRandomLawset(Entity<SiliconLawProviderComponent> entity)
    {
        if (!entity.Comp.UseRandomLawset || entity.Comp.Subverted || entity.Comp.Lawset != null)
            return;

        var station = _station.GetOwningStation(entity.Owner) ?? entity.Owner;
        var lawset = GetStationLawset(station, GetLawUploadTarget(entity.Owner) ?? LawUploadTarget.All, entity.Comp.Laws);
        entity.Comp.Laws = lawset.Id;
        entity.Comp.Lawset = lawset.Laws;
        UpdateCrewLawIndicator(entity.Owner, lawset.Id);
    }

    public (ProtoId<SiliconLawsetPrototype> Id, SiliconLawset Laws) GetStationLawset(
        EntityUid station, LawUploadTarget target, ProtoId<SiliconLawsetPrototype>? fallback = null)
    {
        if (_stationLawsetOverrides.TryGetValue((station, target), out var overridden))
            return (overridden.Id, overridden.Laws.Clone());

        if (!_stationLawsetCache.TryGetValue(station, out var lawset))
        {
            var lawsetId = fallback ?? DefaultCrewLawset;
            if (_randomLawsetWeights.Count > 0)
                lawsetId = _random.Pick(_randomLawsetWeights);

            lawset = (lawsetId, GetLawset(lawsetId));
            _stationLawsetCache[station] = lawset;
        }

        return (lawset.Id, lawset.Laws.Clone());
    }

    private LawUploadTarget? GetLawUploadTarget(EntityUid uid)
    {
        if (HasComp<BorgChassisComponent>(uid))
            return LawUploadTarget.Borgs;

        return HasComp<StationAiCustomizationComponent>(uid) ? LawUploadTarget.Ai : null;
    }

    /// <summary>
    /// Stores a separate snapshot for future spawns and updates eligible station silicons.
    /// The caller is responsible for authenticating the console user.
    /// </summary>
    public int UploadStationLawset(EntityUid station, LawUploadTarget target,
        ProtoId<SiliconLawsetPrototype> id, SiliconLawset lawset, SoundSpecifier? cue = null)
    {
        if (!Enum.IsDefined(target))
            return 0;

        if (target == LawUploadTarget.All)
        {
            _stationLawsetCache[station] = (id, lawset.Clone());
            _stationLawsetOverrides.Remove((station, LawUploadTarget.Ai));
            _stationLawsetOverrides.Remove((station, LawUploadTarget.Borgs));
        }
        else
        {
            _stationLawsetOverrides[(station, target)] = (id, lawset.Clone());
        }

        var count = 0;
        var query = EntityQueryEnumerator<SiliconLawProviderComponent, SiliconLawBoundComponent>();
        while (query.MoveNext(out var uid, out var provider, out _))
        {
            if (!provider.UseRandomLawset || provider.Subverted ||
                _station.GetOwningStation(uid) != station ||
                GetLawUploadTarget(uid) is not { } kind ||
                (target != LawUploadTarget.All && kind != target))
                continue;

            ApplyUploadedLawset((uid, provider), id, lawset, cue);
            count++;
        }

        RaiseLocalEvent(new StationLawsetsChangedEvent(station));
        return count;
    }

    private void UpdateCrewLawIndicator(EntityUid uid, ProtoId<SiliconLawsetPrototype> lawset)
    {
        if (!TryComp<ShowCrewIconsComponent>(uid, out var crewIconComp))
            return;

        crewIconComp.UncertainCrewBorder = DefaultCrewLawset != lawset;
        Dirty(uid, crewIconComp);
    }

    private void ApplyUploadedLawset(Entity<SiliconLawProviderComponent> entity,
        ProtoId<SiliconLawsetPrototype> id, SiliconLawset lawset, SoundSpecifier? cue)
    {
        entity.Comp.Laws = id;
        entity.Comp.Lawset = lawset.Clone();
        UpdateCrewLawIndicator(entity.Owner, id);
        NotifyLawsChanged(entity.Owner, cue);
    }

    private void OnBoundUIOpened(EntityUid uid, SiliconLawBoundComponent component, BoundUIOpenedEvent args)
    {
        UpdateLawsUi((uid, component));
    }

    private void UpdateLawsUi(Entity<SiliconLawBoundComponent> entity)
    {
        TryComp(entity.Owner, out IntrinsicRadioTransmitterComponent? intrinsicRadio);
        var radioChannels = intrinsicRadio?.Channels;

        var state = new SiliconLawBuiState(GetLaws(entity.Owner, entity.Comp).Laws, radioChannels);
        _userInterface.SetUiState(entity.Owner, SiliconLawsUiKey.Key, state);
    }

    public override void NotifyLawsChanged(EntityUid uid, SoundSpecifier? cue = null)
    {
        base.NotifyLawsChanged(uid, cue);

        if (TryComp<SiliconLawBoundComponent>(uid, out var bound))
            UpdateLawsUi((uid, bound));

        if (!TryComp<ActorComponent>(uid, out var actor))
            return;

        var msg = Loc.GetString("laws-update-notify");
        var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", msg));
        _chatManager.ChatMessageToOne(ChatChannel.Server, msg, wrappedMessage, default, false,
            actor.PlayerSession.Channel, colorOverride: Color.Red);

        if (cue != null && _mind.TryGetMind(uid, out var mindId, out _))
            _roles.MindPlaySound(mindId, cue);
    }

    protected override void OnUpdaterInsert(Entity<SiliconLawUpdaterComponent> ent,
        ref EntInsertedIntoContainerMessage args)
    {
        if (HasComp<LawUploadConsoleComponent>(ent))
            return;

        if (!TryComp<SiliconLawProviderComponent>(args.Entity, out var provider))
            return;

        var lawset = provider.Lawset ?? GetLawset(provider.Laws);
        if (ent.Comp.UpdateStationLawset)
        {
            if (_station.GetOwningStation(ent.Owner) is not { } station)
                return;

            UploadStationLawset(station, LawUploadTarget.All, provider.Laws, lawset, provider.LawUploadSound);
            return;
        }

        var query = EntityManager.CompRegistryQueryEnumerator(ent.Comp.Components);
        while (query.MoveNext(out var update))
        {
            if (TryComp<SiliconLawProviderComponent>(update, out var targetProvider))
                ApplyUploadedLawset((update, targetProvider), provider.Laws, lawset, provider.LawUploadSound);
        }
    }
    // SS220 random lawset end
}
