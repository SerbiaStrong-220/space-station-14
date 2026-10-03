using System.Linq;
using Content.Server.Administration;
using Content.Server.Administration.Managers;
using Content.Server.Chat.Managers;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Station.Systems;
using Content.Shared.Administration;
using Content.Shared.Chat;
using Content.Shared.Emag.Systems;
using Content.Shared.GameTicking;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Overlays;
using Content.Shared.Radio.Components;
// SS220 random lawset begin
using Content.Shared.Random.Helpers;
// SS220 random lawset end
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.Laws.Components;
// SS220 random lawset begin
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Silicons.StationAi;
using Content.Shared.SS220.Silicons.Laws;
// SS220 random lawset end
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
// SS220 random lawset begin
using Robust.Shared.Random;
// SS220 random lawset end
using Robust.Shared.Toolshed;

namespace Content.Server.Silicons.Laws;

/// <inheritdoc/>
public sealed class SiliconLawSystem : SharedSiliconLawSystem
{
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedRoleSystem _roles = default!;
    [Dependency] private readonly IBanManager _banManager = default!; // SS220 Antag ban fix
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly EmagSystem _emag = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!; // SS220 Antag ban fix
    // SS220 random lawset begin
    [Dependency] private readonly IRobustRandom _random = default!;

    private readonly Dictionary<EntityUid, (ProtoId<SiliconLawsetPrototype> Id, SiliconLawset Laws)> _stationLawsetCache = new();
    private readonly Dictionary<(EntityUid Station, LawUploadTarget Target),
        (ProtoId<SiliconLawsetPrototype> Id, SiliconLawset Laws)> _stationLawsetOverrides = new();
    // SS220 random lawset end

    private static readonly ProtoId<SiliconLawsetPrototype> DefaultCrewLawset = "Crewsimov";

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SiliconLawBoundComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SiliconLawBoundComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<SiliconLawBoundComponent, ToggleLawsScreenEvent>(OnToggleLawsScreen);
        SubscribeLocalEvent<SiliconLawBoundComponent, BoundUIOpenedEvent>(OnBoundUIOpened);
        SubscribeLocalEvent<SiliconLawBoundComponent, PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);

        SubscribeLocalEvent<SiliconLawProviderComponent, GetSiliconLawsEvent>(OnDirectedGetLaws);
        SubscribeLocalEvent<SiliconLawProviderComponent, IonStormLawsEvent>(OnIonStormLaws);
        SubscribeLocalEvent<SiliconLawProviderComponent, MindAddedMessage>(OnLawProviderMindAdded);
        SubscribeLocalEvent<SiliconLawProviderComponent, MindRemovedMessage>(OnLawProviderMindRemoved);
        SubscribeLocalEvent<SiliconLawProviderComponent, SiliconEmaggedEvent>(OnEmagLawsAdded);
        // SS220 random lawset begin
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        // SS220 random lawset end
    }

    // SS220 random lawset begin
    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _stationLawsetCache.Clear();
        _stationLawsetOverrides.Clear();
    }

    private void InitializeRandomLawset(Entity<SiliconLawProviderComponent> entity)
    {
        // Apply the station default only once. Uploaded and subverted laws take precedence.
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
            var weights = _prototype.EnumeratePrototypes<SiliconLawsetPrototype>()
                .Where(proto => proto.Randomizable && proto.Weight is > 0 && float.IsFinite(proto.Weight.Value))
                .ToDictionary(proto => new ProtoId<SiliconLawsetPrototype>(proto.ID), proto => proto.Weight!.Value);
            if (weights.Count > 0)
                lawsetId = _random.Pick(weights);

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
            _stationLawsetOverrides[(station, target)] = (id, lawset.Clone());

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
    // SS220 random lawset end

    private void OnMapInit(EntityUid uid, SiliconLawBoundComponent component, MapInitEvent args)
    {
        GetLaws(uid, component);
    }

    private void OnMindAdded(EntityUid uid, SiliconLawBoundComponent component, MindAddedMessage args)
    {
        if (!TryComp<ActorComponent>(uid, out var actor))
            return;

        var msg = Loc.GetString("laws-notify");
        var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", msg));
        _chatManager.ChatMessageToOne(ChatChannel.Server, msg, wrappedMessage, default, false, actor.PlayerSession.Channel, colorOverride: Color.FromHex("#5ed7aa"));

        if (!TryComp<SiliconLawProviderComponent>(uid, out var lawcomp))
            return;

        if (!lawcomp.Subverted)
            return;

        var modifedLawMsg = Loc.GetString("laws-notify-subverted");
        var modifiedLawWrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", modifedLawMsg));
        _chatManager.ChatMessageToOne(ChatChannel.Server, modifedLawMsg, modifiedLawWrappedMessage, default, false, actor.PlayerSession.Channel, colorOverride: Color.Red);
    }

    private void OnLawProviderMindAdded(Entity<SiliconLawProviderComponent> ent, ref MindAddedMessage args)
    {
        if (!ent.Comp.Subverted)
            return;
        EnsureSubvertedSiliconRole(args.Mind);
    }

    private void OnLawProviderMindRemoved(Entity<SiliconLawProviderComponent> ent, ref MindRemovedMessage args)
    {
        if (!ent.Comp.Subverted)
            return;
        RemoveSubvertedSiliconRole(args.Mind);

    }


    private void OnToggleLawsScreen(EntityUid uid, SiliconLawBoundComponent component, ToggleLawsScreenEvent args)
    {
        if (args.Handled || !TryComp<ActorComponent>(uid, out var actor))
            return;
        args.Handled = true;

        _userInterface.TryToggleUi(uid, SiliconLawsUiKey.Key, actor.PlayerSession);
    }

    private void OnBoundUIOpened(EntityUid uid, SiliconLawBoundComponent component, BoundUIOpenedEvent args)
    {
        // SS220 random lawset begin
        UpdateLawsUi((uid, component));
        // SS220 random lawset end
    }

    // SS220 random lawset begin
    private void UpdateLawsUi(Entity<SiliconLawBoundComponent> entity)
    {
        TryComp(entity.Owner, out IntrinsicRadioTransmitterComponent? intrinsicRadio);
        var radioChannels = intrinsicRadio?.Channels;

        var state = new SiliconLawBuiState(GetLaws(entity.Owner, entity.Comp).Laws, radioChannels);
        _userInterface.SetUiState(entity.Owner, SiliconLawsUiKey.Key, state);
    }
    // SS220 random lawset end

    private void OnPlayerSpawnComplete(EntityUid uid, SiliconLawBoundComponent component, PlayerSpawnCompleteEvent args)
    {
        component.LastLawProvider = args.Station;
    }

    private void OnDirectedGetLaws(EntityUid uid, SiliconLawProviderComponent component, ref GetSiliconLawsEvent args)
    {
        if (args.Handled)
            return;

        // SS220 random lawset begin
        InitializeRandomLawset((uid, component));
        // SS220 random lawset end

        if (component.Lawset == null)
            component.Lawset = GetLawset(component.Laws);

        args.Laws = component.Lawset;

        args.Handled = true;
    }

    private void OnIonStormLaws(EntityUid uid, SiliconLawProviderComponent component, ref IonStormLawsEvent args)
    {
        // Emagged borgs are immune to ion storm
        if (!_emag.CheckFlag(uid, EmagType.Interaction))
        {
            component.Lawset = args.Lawset;

            // gotta tell player to check their laws
            NotifyLawsChanged(uid, component.LawUploadSound);

            // Show the silicon has been subverted.
            component.Subverted = true;

            // new laws may allow antagonist behaviour so make it clear for admins
            if(_mind.TryGetMind(uid, out var mindId, out _))
                EnsureSubvertedSiliconRole(mindId);

        }
    }

    private void OnEmagLawsAdded(EntityUid uid, SiliconLawProviderComponent component, ref SiliconEmaggedEvent args)
    {
        // SS220 random lawset begin
        InitializeRandomLawset((uid, component));
        // SS220 random lawset end

        if (component.Lawset == null)
            component.Lawset = GetLawset(component.Laws);

        // Show the silicon has been subverted.
        component.Subverted = true;

        // Add the first emag law before the others
        component.Lawset?.Laws.Insert(0, new SiliconLaw
        {
            LawString = Loc.GetString("law-emag-custom", ("name", Name(args.user)), ("title", Loc.GetString(component.Lawset.ObeysTo))),
            Order = 0
        });

        //Add the secrecy law after the others
        component.Lawset?.Laws.Add(new SiliconLaw
        {
            LawString = Loc.GetString("law-emag-secrecy", ("faction", Loc.GetString(component.Lawset.ObeysTo))),
            Order = component.Lawset.Laws.Max(law => law.Order) + 1
        });
    }

    protected override void EnsureSubvertedSiliconRole(EntityUid mindId)
    {
        if (_roles.MindHasRole<SubvertedSiliconRoleComponent>(mindId))
            return;

        // SS220 antag ban
        if (TryComp<MindComponent>(mindId, out var mind)
            && mind.CurrentEntity is { } entity
            && _playerManager.TryGetSessionByEntity(entity, out var session)
            && _banManager.GetJobBans(session.UserId) is { } roleBans
            && roleBans.Contains("SubvertedSilicon"))
        {
            // If user has role ban - kick him out of emagged borg.
            _mind.TransferTo(mindId, null);

            var ghostRole = EnsureComp<GhostRoleComponent>(entity);
            EnsureComp<GhostTakeoverAvailableComponent>(entity);
            ghostRole.RoleName = Loc.GetString("roles-antag-subverted-silicon-name");
            ghostRole.RoleDescription = Loc.GetString("roles-antag-subverted-silicon-name");
            ghostRole.RoleRules = Loc.GetString("roles-antag-subverted-silicon-objective");
        }

        base.EnsureSubvertedSiliconRole(mindId);

        if (!_roles.MindHasRole<SubvertedSiliconRoleComponent>(mindId))
            _roles.MindAddRole(mindId, "MindRoleSubvertedSilicon", silent: true);
    }

    protected override void RemoveSubvertedSiliconRole(EntityUid mindId)
    {
        base.RemoveSubvertedSiliconRole(mindId);

        if (_roles.MindHasRole<SubvertedSiliconRoleComponent>(mindId))
            _roles.MindRemoveRole<SubvertedSiliconRoleComponent>(mindId);
    }

    public SiliconLawset GetLaws(EntityUid uid, SiliconLawBoundComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return new SiliconLawset();

        var ev = new GetSiliconLawsEvent(uid);

        RaiseLocalEvent(uid, ref ev);
        if (ev.Handled)
        {
            component.LastLawProvider = uid;
            return ev.Laws;
        }

        var xform = Transform(uid);

        if (_station.GetOwningStation(uid, xform) is { } station)
        {
            RaiseLocalEvent(station, ref ev);
            if (ev.Handled)
            {
                component.LastLawProvider = station;
                return ev.Laws;
            }
        }

        if (xform.GridUid is { } grid)
        {
            RaiseLocalEvent(grid, ref ev);
            if (ev.Handled)
            {
                component.LastLawProvider = grid;
                return ev.Laws;
            }
        }

        if (component.LastLawProvider == null ||
            Deleted(component.LastLawProvider) ||
            Terminating(component.LastLawProvider.Value))
        {
            component.LastLawProvider = null;
        }
        else
        {
            RaiseLocalEvent(component.LastLawProvider.Value, ref ev);
            if (ev.Handled)
            {
                return ev.Laws;
            }
        }

        RaiseLocalEvent(ref ev);
        return ev.Laws;
    }

    public override void NotifyLawsChanged(EntityUid uid, SoundSpecifier? cue = null)
    {
        base.NotifyLawsChanged(uid, cue);

        // SS220 random lawset begin
        // Refresh an existing laws screen without reopening it.
        if (TryComp<SiliconLawBoundComponent>(uid, out var bound))
            UpdateLawsUi((uid, bound));
        // SS220 random lawset end

        if (!TryComp<ActorComponent>(uid, out var actor))
            return;

        var msg = Loc.GetString("laws-update-notify");
        var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", msg));
        _chatManager.ChatMessageToOne(ChatChannel.Server, msg, wrappedMessage, default, false, actor.PlayerSession.Channel, colorOverride: Color.Red);

        if (cue != null && _mind.TryGetMind(uid, out var mindId, out _))
            _roles.MindPlaySound(mindId, cue);
    }

    /// <summary>
    /// Extract all the laws from a lawset's prototype ids.
    /// </summary>
    public SiliconLawset GetLawset(ProtoId<SiliconLawsetPrototype> lawset)
    {
        var proto = _prototype.Index(lawset);
        var laws = new SiliconLawset()
        {
            Laws = new List<SiliconLaw>(proto.Laws.Count)
        };
        foreach (var law in proto.Laws)
        {
            laws.Laws.Add(_prototype.Index<SiliconLawPrototype>(law).ShallowClone());
        }
        laws.ObeysTo = proto.ObeysTo;

        return laws;
    }

    /// <summary>
    /// Set the laws of a silicon entity while notifying the player.
    /// </summary>
    public void SetLaws(List<SiliconLaw> newLaws, EntityUid target, SoundSpecifier? cue = null)
    {
        if (!TryComp<SiliconLawProviderComponent>(target, out var component))
            return;

        if (component.Lawset == null)
            component.Lawset = new SiliconLawset();

        // SS220 random lawset begin
        // Each recipient must own its laws after an upload.
        component.Lawset.Laws = newLaws.Select(law => law.ShallowClone()).ToList();
        // SS220 random lawset end
        NotifyLawsChanged(target, cue);
    }

    protected override void OnUpdaterInsert(Entity<SiliconLawUpdaterComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        // SS220 random lawset begin
        // Interactive upload consoles require explicit confirmation.
        if (HasComp<LawUploadConsoleComponent>(ent))
            return;
        // SS220 random lawset end

        if (!TryComp<SiliconLawProviderComponent>(args.Entity, out var provider))
            return;

        var lawset = provider.Lawset ?? GetLawset(provider.Laws);
        // SS220 random lawset begin
        if (ent.Comp.UpdateStationLawset)
        {
            if (_station.GetOwningStation(ent.Owner) is not { } station)
                return;

            UploadStationLawset(station, LawUploadTarget.All, provider.Laws, lawset, provider.LawUploadSound);
            return;
        }
        // Other updaters retain their explicitly configured target selection.
        var query = EntityManager.CompRegistryQueryEnumerator(ent.Comp.Components);
        while (query.MoveNext(out var update))
        {
            if (TryComp<SiliconLawProviderComponent>(update, out var targetProvider))
                ApplyUploadedLawset((update, targetProvider), provider.Laws, lawset, provider.LawUploadSound);
        }
        // SS220 random lawset end
    }
}

[ToolshedCommand, AdminCommand(AdminFlags.Admin)]
public sealed class LawsCommand : ToolshedCommand
{
    private SiliconLawSystem? _law;

    [CommandImplementation("list")]
    public IEnumerable<EntityUid> List()
    {
        var query = EntityManager.EntityQueryEnumerator<SiliconLawBoundComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            yield return uid;
        }
    }

    [CommandImplementation("get")]
    public IEnumerable<string> Get([PipedArgument] EntityUid lawbound)
    {
        _law ??= GetSys<SiliconLawSystem>();

        foreach (var law in _law.GetLaws(lawbound).Laws)
        {
            yield return $"law {law.LawIdentifierOverride ?? law.Order.ToString()}: {Loc.GetString(law.LawString)}";
        }
    }
}
