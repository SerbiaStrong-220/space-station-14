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
using Content.Shared.Radio.Components;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.Laws.Components;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
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
    [Dependency] private readonly IRobustRandom _random = default!; // SS220 Random lawset

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
    }

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
        TryComp(uid, out IntrinsicRadioTransmitterComponent? intrinsicRadio);
        var radioChannels = intrinsicRadio?.Channels;

        var state = new SiliconLawBuiState(GetLaws(uid).Laws, radioChannels);
        _userInterface.SetUiState(args.Entity, SiliconLawsUiKey.Key, state);
    }

    private void OnPlayerSpawnComplete(EntityUid uid, SiliconLawBoundComponent component, PlayerSpawnCompleteEvent args)
    {
        component.LastLawProvider = args.Station;
    }

    private void OnDirectedGetLaws(EntityUid uid, SiliconLawProviderComponent component, ref GetSiliconLawsEvent args)
    {
        if (args.Handled)
            return;

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
        var possibleLawsets = new List<SiliconLawset>(); // SS220 Random lawset 

        CollectLawset(uid, ev, possibleLawsets); // SS220 Random lawset 

        var xform = Transform(uid);

        if (_station.GetOwningStation(uid, xform) is { } station)
        {
            CollectLawset(station, ev, possibleLawsets); // SS220 Random lawset 
        }

        if (xform.GridUid is { } grid)
        {
            CollectLawset(grid, ev, possibleLawsets); // SS220 Random lawset 
        }

        if (component.LastLawProvider == null ||
            Deleted(component.LastLawProvider) ||
            Terminating(component.LastLawProvider.Value))
        {
            component.LastLawProvider = null;
        }
        // SS220 Random lawset begin
        else
        {
            CollectLawset(component.LastLawProvider.Value, ev, possibleLawsets);
        }

        RaiseLocalEvent(ref ev);
        if (ev.Handled)
        {
            possibleLawsets.Add(ev.Laws);
        }

        if (possibleLawsets.Count > 0)
        {
            var randomIndex = _random.Next(possibleLawsets.Count);
            var selected = possibleLawsets[randomIndex];

            if (selected.Provider != null)
            {
                component.LastLawProvider = selected.Provider;
            }

            return selected;
        }

        return new SiliconLawset();
    }

    private void CollectLawset(EntityUid provider, GetSiliconLawsEvent ev, List<SiliconLawset> list)
    {
        ev.Handled = false;
        ev.Laws = new SiliconLawset();
        ev.Provider = provider;

        RaiseLocalEvent(provider, ref ev);

        if (ev.Handled)
        {
            list.Add(ev.Laws);
        }
        // SS220 Random lawset end
    }

    public override void NotifyLawsChanged(EntityUid uid, SoundSpecifier? cue = null)
    {
        base.NotifyLawsChanged(uid, cue);

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
            Laws = new List<SiliconLaw>(proto.Laws.Count), // SS220 Random lawset
            ObeysTo = proto.ObeysTo // SS220 Random lawset
        };

        foreach (var law in proto.Laws)
        {
            laws.Laws.Add(_prototype.Index<SiliconLawPrototype>(law).ShallowClone());
        }

        return laws;
    }
    // SS220 Random lawset begin
    /// <summary>
    /// Select a random set of laws according to their weightsApcPowerReceiver
    /// </summary>
    public SiliconLawset GetWeightedRandomLawset()
    {
        var weightedLawsets = new Dictionary<ProtoId<SiliconLawsetPrototype>, float>();
        float totalWeight = 0f;

        foreach (var lawsetProto in _prototype.EnumeratePrototypes<SiliconLawsetPrototype>())
        {
            var weight = lawsetProto.Weight;

            if (weight <= 0)
                continue;

            weightedLawsets[lawsetProto.ID] = weight;
            totalWeight += weight;
        }

        if (weightedLawsets.Count == 0 || totalWeight <= 0)
            return new SiliconLawset();

        var randomValue = _random.NextFloat(0, totalWeight);
        var accumulated = 0f;

        foreach (var kvp in weightedLawsets)
        {
            accumulated += kvp.Value;
            if (randomValue <= accumulated)
                return GetLawset(kvp.Key);
        }

        return GetLawset(weightedLawsets.Keys.First());
    }
    // SS220 Random lawset end

    /// <summary>
    /// Set the laws of a silicon entity while notifying the player.
    /// </summary>
    public void SetLaws(List<SiliconLaw> newLaws, EntityUid target, SoundSpecifier? cue = null)
    {
        if (!TryComp<SiliconLawProviderComponent>(target, out var component))
            return;

        if (component.Lawset == null)
            component.Lawset = new SiliconLawset();

        component.Lawset.Laws = newLaws;
        NotifyLawsChanged(target, cue);
    }

    protected override void OnUpdaterInsert(Entity<SiliconLawUpdaterComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        // TODO: Prediction dump this
        if (!TryComp<SiliconLawProviderComponent>(args.Entity, out var provider))
            return;

        var lawset = provider.Lawset ?? GetLawset(provider.Laws);

        var query = EntityManager.CompRegistryQueryEnumerator(ent.Comp.Components);

        while (query.MoveNext(out var update))
        {
            SetLaws(lawset.Laws, update, provider.LawUploadSound);
        }
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
