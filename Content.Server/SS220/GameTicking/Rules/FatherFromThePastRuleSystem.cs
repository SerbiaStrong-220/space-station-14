using System.Numerics;
using Content.Server.Antag;
using Content.Server.Chat.Managers;
using Content.Server.Cloning;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Objectives.Components;
using Content.Shared.Chat;
using Content.Shared.GameTicking.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Objectives.Systems;
using Content.Shared.SS220.Antag;
using Content.Shared.VendingMachines;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.GameTicking.Rules;

public sealed class FatherFromThePastRuleSystem : GameRuleSystem<FatherFromThePastRuleComponent>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly CloningSystem _cloning = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly TargetSystem _target = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FatherFromThePastRuleComponent, AntagSelectEntityEvent>(OnAntagSelectEntity);
        SubscribeLocalEvent<FatherFromThePastRuleComponent, AfterAntagEntitySelectedEvent>(AfterAntagEntitySelected);
    }

    protected override void Started(EntityUid uid, FatherFromThePastRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (_target.GetAliveHumans().Count == 0)
        {
            Log.Info("No alive players to spawn Father From The Past from! Ending gamerule.");
            ForceEndSelf(uid, gameRule);
        }
    }

    private void OnAntagSelectEntity(Entity<FatherFromThePastRuleComponent> ent, ref AntagSelectEntityEvent args)
    {
        if (args.Session?.AttachedEntity is not { } spawner)
            return;

        if (ent.Comp.OriginalBody != null)
        {
            if (Deleted(ent.Comp.OriginalBody.Value) || !_mind.TryGetMind(ent.Comp.OriginalBody.Value, out var originalMindId, out _))
            {
                Log.Warning("Could not find mind of target player for Father From The Past!");
                return;
            }
            ent.Comp.OriginalMind = originalMindId;
        }
        else
        {
            var valid = new List<Entity<MindComponent>>();
            foreach (var humanoid in _target.GetAliveHumans())
            {
                if (!HasComp<ParadoxCloneBlacklistComponent>(humanoid.Comp.OwnedEntity))
                    valid.Add(humanoid);
            }

            if (valid.Count == 0)
            {
                Log.Warning(Loc.GetString("father-from-the-past-log-no-child"));
                return;
            }

            var picked = _random.Pick(valid);
            ent.Comp.OriginalMind = picked;
            ent.Comp.OriginalBody = picked.Comp.OwnedEntity;
        }

        var coords = TryGetVendingSpawn(ent) ?? _transform.GetMapCoordinates(spawner);

        if (ent.Comp.OriginalBody == null ||
            !_cloning.TryCloning(ent.Comp.OriginalBody.Value, coords, ent.Comp.Settings, out var clone) ||
            clone == null)
        {
            Log.Error(Loc.GetString("father-from-the-past-log-clone-failed", ("target", $"{ToPrettyString(ent.Comp.OriginalBody)}")));
            return;
        }

        _metaData.SetEntityName(clone.Value, Name(ent.Comp.OriginalBody.Value));
        _humanoid.SetSex(clone.Value, Sex.Male);

        var targetComp = EnsureComp<TargetOverrideComponent>(clone.Value);
        targetComp.Target = ent.Comp.OriginalMind;

        args.Entity = clone;
    }

    private void AfterAntagEntitySelected(Entity<FatherFromThePastRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        if (ent.Comp.OriginalMind == null)
            return;

        if (TryComp<MindComponent>(ent.Comp.OriginalMind.Value, out var childMind)
            && childMind.UserId is { } userId
            && _player.TryGetSessionById(userId, out var session))
        {
            var msg = Loc.GetString("father-from-the-past-child-notification");
            _chat.ChatMessageToOne(ChatChannel.Server, msg, msg, default, false, session.Channel);
        }
    }

    private MapCoordinates? TryGetVendingSpawn(Entity<FatherFromThePastRuleComponent> ent)
    {
        var machines = new List<EntityUid>();
        var query = EntityQueryEnumerator<VendingMachineComponent, MetaDataComponent>();
        while (query.MoveNext(out var uid, out _, out var meta))
        {
            if (meta.EntityPrototype != null && ent.Comp.VendingMachines.Contains(meta.EntityPrototype.ID))
                machines.Add(uid);
        }

        if (machines.Count == 0)
            return null;

        var machine = _random.Pick(machines);
        var baseCoords = _transform.GetMapCoordinates(machine);

        Vector2[] offsets = { new(0, -1), new(0, 1), new(1, 0), new(-1, 0) };
        return baseCoords.Offset(_random.Pick(offsets));
    }
}
