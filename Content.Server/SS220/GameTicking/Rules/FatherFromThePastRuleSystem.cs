using System.Numerics;
using Content.Server.Antag;
using Content.Server.Chat.Managers;
using Content.Server.Cloning;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Objectives.Components;
using Content.Shared.Chat;
using Content.Shared.GameTicking.Components;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Maps;
using Content.Shared.Mind;
using Content.Shared.Objectives.Systems;
using Content.Shared.Physics;
using Content.Shared.SS220.Antag;
using Content.Shared.VendingMachines;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.GameTicking.Rules;

public sealed partial class FatherFromThePastRuleSystem : GameRuleSystem<FatherFromThePastRuleComponent>
{
    private const string NoAliveHumansLog = "No alive players to spawn Father From The Past from! Ending gamerule.";
    private const string NoTargetMindLog = "Could not find mind of target player for Father From The Past!";
    private const string FallbackSpecies = "Human";
    private const string NoVendingMachineLog = "No cigarette vending machine to spawn Father From The Past at! Ending gamerule.";

    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IChatManager _chat = default!;
    [Dependency] private CloningSystem _cloning = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private TargetSystem _target = default!;
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private ISharedPlayerManager _player = default!;
    [Dependency] private HumanoidProfileSystem _humanoidProfile = default!;
    [Dependency] private NamingSystem _naming = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private TurfSystem _turf = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FatherFromThePastRuleComponent, AntagSelectEntityEvent>(OnAntagSelectEntity);
        SubscribeLocalEvent<FatherFromThePastRuleComponent, AntagSelectLocationEvent>(OnAntagSelectLocation);
        SubscribeLocalEvent<FatherFromThePastRuleComponent, AfterAntagEntitySelectedEvent>(AfterAntagEntitySelected);
    }

    protected override void Started(EntityUid uid, FatherFromThePastRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (_target.GetAliveHumans().Count == 0)
        {
            Log.Info(NoAliveHumansLog);
            ForceEndSelf(uid, gameRule);
            return;
        }

        if (GetVendingMachines((uid, component)).Count == 0)
        {
            Log.Info(NoVendingMachineLog);
            ForceEndSelf(uid, gameRule);
        }
    }

    private void OnAntagSelectEntity(Entity<FatherFromThePastRuleComponent> ent, ref AntagSelectEntityEvent args)
    {
        if (args.Session?.AttachedEntity is not { } spawner)
            return;

        if (!TryResolveTarget(ent) || ent.Comp.OriginalBody is not { } originalBody)
            return;

        var coords = TryGetVendingSpawn(ent) ?? _transform.GetMapCoordinates(spawner);

        if (!_cloning.TryCloning(originalBody, coords, ent.Comp.Settings, out var clone) || clone == null)
        {
            Log.Error(Loc.GetString("father-from-the-past-log-clone-failed", ("target", $"{ToPrettyString(originalBody)}")));
            return;
        }

        _metaData.SetEntityName(clone.Value, BuildFatherName(clone.Value, originalBody));
        _humanoidProfile.SetSex(clone.Value, Sex.Male, Gender.Male);
        EnsureComp<TargetOverrideComponent>(clone.Value).Target = ent.Comp.OriginalMind;

        args.Entity = clone;
    }

    private void AfterAntagEntitySelected(Entity<FatherFromThePastRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        var name = Name(args.EntityUid);
        var renamedEv = new EntityRenamedEvent(args.EntityUid, name, name);
        RaiseLocalEvent(args.EntityUid, ref renamedEv, true);

        if (ent.Comp.SpawnEffect is { } spawnEffect)
            Spawn(spawnEffect, _transform.GetMapCoordinates(args.EntityUid));

        if (ent.Comp.OriginalMind is not { } childMindId
            || !TryComp<MindComponent>(childMindId, out var childMind)
            || childMind.UserId is not { } userId
            || !_player.TryGetSessionById(userId, out var session))
        {
            return;
        }

        var msg = Loc.GetString("father-from-the-past-child-notification");
        _chat.ChatMessageToOne(ChatChannel.Server, msg, msg, default, false, session.Channel);
    }

    private void OnAntagSelectLocation(Entity<FatherFromThePastRuleComponent> ent, ref AntagSelectLocationEvent args)
    {
        if (!args.Handled && TryGetVendingSpawn(ent) is { } coords)
            args.Coordinates.Add(coords);
    }

    private bool TryResolveTarget(Entity<FatherFromThePastRuleComponent> ent)
    {
        if (ent.Comp.OriginalBody is { } body)
        {
            if (Deleted(body) || !_mind.TryGetMind(body, out var bodyMind, out _))
            {
                Log.Warning(NoTargetMindLog);
                return false;
            }

            ent.Comp.OriginalMind = bodyMind;
            return true;
        }

        var valid = new List<Entity<MindComponent>>();
        foreach (var humanoid in _target.GetAliveHumans())
        {
            if (!HasComp<ParadoxCloneBlacklistComponent>(humanoid.Comp.OwnedEntity))
                valid.Add(humanoid);
        }

        if (valid.Count == 0)
        {
            Log.Warning(Loc.GetString("father-from-the-past-log-no-child"));
            return false;
        }

        var picked = _random.Pick(valid);
        ent.Comp.OriginalMind = picked;
        ent.Comp.OriginalBody = picked.Comp.OwnedEntity;
        return true;
    }

    private string BuildFatherName(EntityUid clone, EntityUid originalBody)
    {
        var species = TryComp<HumanoidProfileComponent>(clone, out var profile) ? profile.Species.Id : FallbackSpecies;

        var parts = Name(originalBody).Split(' ', 2);
        if (parts.Length == 2 && _prototype.TryIndex<SpeciesPrototype>(species, out var speciesProto))
            return $"{_naming.GetFirstName(speciesProto, Gender.Male)} {parts[1]}";

        return _naming.GetName(species, Gender.Male);
    }

    private List<EntityUid> GetVendingMachines(Entity<FatherFromThePastRuleComponent> ent)
    {
        var machines = new List<EntityUid>();
        var query = EntityQueryEnumerator<VendingMachineComponent, MetaDataComponent>();
        while (query.MoveNext(out var uid, out _, out var meta))
        {
            if (meta.EntityPrototype != null && ent.Comp.VendingMachines.Contains(meta.EntityPrototype.ID))
                machines.Add(uid);
        }

        return machines;
    }

    private MapCoordinates? TryGetVendingSpawn(Entity<FatherFromThePastRuleComponent> ent)
    {
        var machines = GetVendingMachines(ent);
        if (machines.Count == 0)
            return null;

        _random.Shuffle(machines);
        var offsets = new[] { new Vector2(0, -1), new Vector2(0, 1), new Vector2(1, 0), new Vector2(-1, 0) };

        foreach (var machine in machines)
        {
            var baseCoords = Transform(machine).Coordinates;
            _random.Shuffle(offsets);

            foreach (var offset in offsets)
            {
                if (_turf.GetTileRef(baseCoords.Offset(offset)) is not { } tile
                    || tile.Tile.IsEmpty
                    || _turf.IsTileBlocked(tile, CollisionGroup.MobMask))
                {
                    continue;
                }

                return _transform.ToMapCoordinates(_turf.GetTileCenter(tile));
            }
        }

        return null;
    }
}
