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
using Robust.Shared.Random;
using Robust.Shared.Toolshed;

namespace Content.Server.Silicons.Laws;

/// <inheritdoc/>
public sealed class SiliconLawSystem : SharedSiliconLawSystem
{
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedRoleSystem _roles = default!;
    [Dependency] private readonly IRobustRandom _random = default!; // SS220 random lawset
    [Dependency] private readonly IBanManager _banManager = default!; // SS220 Antag ban fix
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly EmagSystem _emag = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!; // SS220 Antag ban fix

    // SS220 random lawset begin - кэш для хранения выбранных lawsets
    private Dictionary<EntityUid, ProtoId<SiliconLawsetPrototype>> _stationLawsetCache = new();
    // SS220 random lawset end

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

        // SS220 random lawset begin - очистка кэша при ресете раунда
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        // SS220 random lawset end
    }

    // SS220 random lawset begin
    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _stationLawsetCache.Clear();
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

        // SS220 random lawset begin - выбираем lawset в зависимости от конфигурации
        if (component.Lawset == null)
        {
            if (component.UseRandomLawset)
            {
                // Для киборгов с рандомным lawset
                var stationLawset = GetStationLawset(uid);
                if (stationLawset != null)
                {
                    // Используем сохраненный lawset для станции
                    component.Laws = stationLawset.Value;
                    component.Lawset = GetLawset(stationLawset.Value);
                }
                else
                {
                    // Если для станции еще нет lawset, выбираем случайный
                    var randomLawset = SelectRandomLawset();
                    if (randomLawset != null)
                    {
                        component.Laws = randomLawset.Value;
                        component.Lawset = GetLawset(randomLawset.Value);

                        // Сохраняем выбранный lawset для станции
                        SaveStationLawset(uid, randomLawset.Value);
                    }
                    else
                    {
                        // Fallback на дефолтный
                        component.Lawset = GetLawset(component.Laws);
                    }
                }
            }
            else
            {
                // Для киборгов с фиксированным lawset
                component.Lawset = GetLawset(component.Laws);
            }
        }
        // SS220 random lawset end

        args.Laws = component.Lawset;
        args.Handled = true;
    }

    // SS220 random lawset begin
    /// <summary>
    /// Получает сохраненный lawset для станции
    /// </summary>
    private ProtoId<SiliconLawsetPrototype>? GetStationLawset(EntityUid uid)
    {
        // Ищем станцию для этого entity
        if (_station.GetOwningStation(uid) is { } station)
        {
            if (_stationLawsetCache.TryGetValue(station, out var lawset))
            {
                return lawset;
            }
        }

        // Проверяем глобальный кэш (если entity сама станция)
        if (_stationLawsetCache.TryGetValue(uid, out var globalLawset))
        {
            return globalLawset;
        }

        return null;
    }

    /// <summary>
    /// Сохраняет lawset для станции
    /// </summary>
    private void SaveStationLawset(EntityUid uid, ProtoId<SiliconLawsetPrototype> lawset)
    {
        // Сохраняем для станции
        if (_station.GetOwningStation(uid) is { } station)
        {
            _stationLawsetCache[station] = lawset;
        }
        else
        {
            // Если не можем найти станцию, сохраняем для самого entity
            _stationLawsetCache[uid] = lawset;
        }
    }
    // SS220 random lawset end

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
            if (_mind.TryGetMind(uid, out var mindId, out _))
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

    // SS220 random lawset begin
    /// <summary>
    /// Selects a random lawset with weights from all available lawsets.
    /// </summary>
    public ProtoId<SiliconLawsetPrototype>? SelectRandomLawset()
    {
        // Get all lawsets that are randomizable
        var allLawsets = _prototype.EnumeratePrototypes<SiliconLawsetPrototype>()
            .Where(proto => proto.Randomizable)
            .ToList();

        if (allLawsets.Count == 0)
            return null;

        float totalWeight = 0f;
        foreach (var lawset in allLawsets)
        {
            totalWeight += lawset.Weight;
        }

        if (totalWeight <= 0)
            return allLawsets[0].ID;

        var randomValue = _random.NextFloat() * totalWeight;
        float cumulative = 0f;

        foreach (var lawset in allLawsets)
        {
            cumulative += lawset.Weight;
            if (randomValue <= cumulative)
            {
                return lawset.ID;
            }
        }

        return allLawsets[0].ID;
    }
    // SS220 random lawset end

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

    // SS220 random lawset begin
    /// <summary>
    /// Sets a random lawset for a silicon entity.
    /// </summary>
    public void SetRandomLawset(EntityUid uid, SiliconLawProviderComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var randomLawset = SelectRandomLawset();
        if (randomLawset != null)
        {
            component.Laws = randomLawset.Value;
            component.Lawset = null; // Reset cached lawset

            // Сохраняем выбранный lawset для станции
            SaveStationLawset(uid, randomLawset.Value);

            // Force refresh laws
            var laws = GetLaws(uid);
            NotifyLawsChanged(uid);
        }
    }

    /// <summary>
    /// Получает текущий lawset для станции
    /// </summary>
    public ProtoId<SiliconLawsetPrototype>? GetCurrentStationLawset(EntityUid station)
    {
        if (_stationLawsetCache.TryGetValue(station, out var lawset))
        {
            return lawset;
        }
        return null;
    }

    /// <summary>
    /// Устанавливает lawset для станции
    /// </summary>
    public void SetStationLawset(EntityUid station, ProtoId<SiliconLawsetPrototype> lawset)
    {
        _stationLawsetCache[station] = lawset;
    }

    /// <summary>
    /// Включает или выключает использование случайного lawset для киборга
    /// </summary>
    public void SetUseRandomLawset(EntityUid uid, bool useRandom, SiliconLawProviderComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.UseRandomLawset = useRandom;
        component.Lawset = null; // Сбрасываем кэш, чтобы при следующем обращении был выбран новый lawset
    }
    // SS220 random lawset end
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

    // SS220 random lawset begin
    [CommandImplementation("setrandom")]
    public void SetRandom([PipedArgument] EntityUid entity)
    {
        _law ??= GetSys<SiliconLawSystem>();
        _law.SetRandomLawset(entity);
    }

    [CommandImplementation("getstationlawset")]
    public string? GetStationLawset([PipedArgument] EntityUid station)
    {
        _law ??= GetSys<SiliconLawSystem>();
        var lawset = _law.GetCurrentStationLawset(station);
        return lawset?.ToString();
    }

    [CommandImplementation("setstationlawset")]
    public void SetStationLawset([PipedArgument] EntityUid station, string lawsetId)
    {
        _law ??= GetSys<SiliconLawSystem>();
        var lawset = new ProtoId<SiliconLawsetPrototype>(lawsetId);
        _law.SetStationLawset(station, lawset);
    }

    [CommandImplementation("userandom")]
    public void UseRandom([PipedArgument] EntityUid entity, bool useRandom)
    {
        _law ??= GetSys<SiliconLawSystem>();
        _law.SetUseRandomLawset(entity, useRandom);
    }
    // SS220 random lawset end
}
