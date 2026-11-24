// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using Content.Server.Antag.Components;
using Content.Shared.Mind;
using Content.Shared.NPC.Prototypes;
using Content.Shared.Roles;
using Content.Shared.Strip.Components;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Zombies;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.AntagRemoval;

public sealed class RemoveAntagGrantSnapshot
{
    public readonly HashSet<EntityUid> ObjectivesBefore = new();
    public readonly HashSet<EntityUid> CreatedEntities = new();
    public readonly HashSet<string> GearPrototypes = new();
    public readonly List<string> RoleIds = new();
    public EntityUid Rule;
    public EntityUid? MindAfterGrant;
    // -1 means a conversion or an ambiguous/unresolved selection definition.
    public int SelectionDefinitionIndex = -1;
    public bool HadThieving;
    public bool HadPacified;
    public bool HadPatientZero;
    public bool HadPendingZombie;
    public bool HadZombifyOnDeath;
    public bool HadIncurableZombie;
    public readonly HashSet<ProtoId<NpcFactionPrototype>> FactionsBefore = new();
}

public sealed partial class RemoveAntagSystem
{
    public RemoveAntagGrantSnapshot BeginGrant(EntityUid rule, EntityUid body, AntagSelectionDefinition definition)
    {
        var snap = new RemoveAntagGrantSnapshot
        {
            Rule = rule,
            HadThieving = HasComp<ThievingComponent>(body),
            HadPacified = HasComp<PacifiedComponent>(body),
            HadPatientZero = HasComp<InitialInfectedComponent>(body),
            HadPendingZombie = HasComp<PendingZombieComponent>(body),
            HadZombifyOnDeath = HasComp<ZombifyOnDeathComponent>(body),
            HadIncurableZombie = HasComp<IncurableZombieComponent>(body),
        };

        if (TryComp<AntagSelectionComponent>(rule, out var selection))
        {
            for (var i = 0; i < selection.Definitions.Count; i++)
            {
                if (!EqualityComparer<AntagSelectionDefinition>.Default.Equals(
                        selection.Definitions[i], definition))
                    continue;

                if (snap.SelectionDefinitionIndex != -1)
                {
                    snap.SelectionDefinitionIndex = -1;
                    break;
                }

                snap.SelectionDefinitionIndex = i;
            }
        }

        snap.FactionsBefore.UnionWith(GetFactionSnapshot(body));
        foreach (var role in definition.MindRoles ?? new())
            snap.RoleIds.Add(role.Id);

        if (EntityManager.System<SharedMindSystem>().TryGetMind(body, out _, out var mind))
            snap.ObjectivesBefore.UnionWith(mind.Objectives);

        if (definition.StartingGear is { } gearId && _prototypes.TryIndex(gearId, out StartingGearPrototype? gear))
        {
            foreach (var item in gear.Equipment.Values)
                snap.GearPrototypes.Add(item.Id);

            foreach (var item in gear.Inhand)
                snap.GearPrototypes.Add(item.Id);

            foreach (var storage in gear.Storage.Values)
                foreach (var item in storage)
                    snap.GearPrototypes.Add(item.Id);
        }

        var origin = EnsureComp<AntagRemovalGrantComponent>(body);
        origin.FactionsBeforeFirstAntag ??= new HashSet<ProtoId<NpcFactionPrototype>>(snap.FactionsBefore);

        _activeGrantSnapshots.Push(snap);
        return snap;
    }

    public void EndGrant(EntityUid body, RemoveAntagGrantSnapshot snapshot)
    {
        // Stop tracking before recalculation and any cleanup can spawn entities.
        if (_activeGrantSnapshots.Count == 0 || !ReferenceEquals(_activeGrantSnapshots.Peek(), snapshot))
            throw new InvalidOperationException("Mismatched antagonist equipment grant scope.");

        _activeGrantSnapshots.Pop();

        var origin = EnsureComp<AntagRemovalGrantComponent>(body);
        var currentObjectives = new HashSet<EntityUid>();
        if (EntityManager.System<SharedMindSystem>().TryGetMind(body, out var grantedMindId, out var mind))
        {
            snapshot.MindAfterGrant = grantedMindId;
            currentObjectives.UnionWith(mind.Objectives);
        }

        currentObjectives.ExceptWith(snapshot.ObjectivesBefore);

        var newGear = new HashSet<EntityUid>();
        foreach (var uid in snapshot.CreatedEntities)
        {
            if (TerminatingOrDeleted(uid) ||
                MetaData(uid).EntityPrototype is not { } prototype ||
                !snapshot.GearPrototypes.Contains(prototype.ID))
                continue;

            newGear.Add(uid);
        }

        var thiefGrant = snapshot.RoleIds.Contains(RemoveAntagRoles.Thief);
        newGear.RemoveWhere(uid => !IsAttachedTo(uid, body) &&
            !(thiefGrant && (MetaData(uid).EntityPrototype?.ID is "SatchelThief" or "ThiefBeacon")));

        var factionsAfter = GetFactionSnapshot(body);
        foreach (var roleId in snapshot.RoleIds)
        {
            if (!origin.Grants.TryGetValue(roleId, out var grant))
                origin.Grants[roleId] = grant = new AntagGrantRecord { RoleId = roleId, GameRule = snapshot.Rule };

            grant.GameRule = snapshot.Rule;
            grant.SelectionDefinitionIndex = snapshot.SelectionDefinitionIndex;

            if (grant.GrantedMindId is { } previousMind && previousMind != snapshot.MindAfterGrant)
                grant.VerifiedProvenance = false;
            else
            {
                grant.GrantedMindId = snapshot.MindAfterGrant;
                grant.VerifiedProvenance = snapshot.MindAfterGrant is not null;
            }

            grant.Objectives.UnionWith(currentObjectives);
            grant.Gear.UnionWith(newGear);
            grant.FactionsAdded.UnionWith(factionsAfter.Except(snapshot.FactionsBefore));
            grant.FactionsRemoved.UnionWith(snapshot.FactionsBefore.Except(factionsAfter));
            grant.GrantedThieving |= !snapshot.HadThieving && HasComp<ThievingComponent>(body);
            grant.GrantedPatientZero |= !snapshot.HadPatientZero && HasComp<InitialInfectedComponent>(body);
            grant.GrantedPendingZombie |= !snapshot.HadPendingZombie && HasComp<PendingZombieComponent>(body);
            grant.GrantedZombifyOnDeath |= !snapshot.HadZombifyOnDeath && HasComp<ZombifyOnDeathComponent>(body);
            grant.GrantedIncurableZombie |= !snapshot.HadIncurableZombie && HasComp<IncurableZombieComponent>(body);
            grant.GrantedPacified |= !snapshot.HadPacified && HasComp<PacifiedComponent>(body);
        }
    }

    public void CancelGrant(RemoveAntagGrantSnapshot snapshot)
    {
        if (_activeGrantSnapshots.TryPeek(out var active) && ReferenceEquals(active, snapshot))
            _activeGrantSnapshots.Pop();
    }

    private HashSet<ProtoId<NpcFactionPrototype>> GetFactionSnapshot(EntityUid body)
        => _factions.SnapshotFactions(body);

    /// <summary>For converts who never enter AntagSelectionSystem.MakeAntag.</summary>
    public void BeforeCultConversion(EntityUid body)
    {
        var origin = EnsureComp<AntagRemovalGrantComponent>(body);
        var factions = GetFactionSnapshot(body);
        origin.FactionsBeforeFirstAntag ??= new HashSet<ProtoId<NpcFactionPrototype>>(factions);
        origin.FactionsBeforeCultConversion = factions;
        origin.HadPacifiedBeforeCultConversion = HasComp<PacifiedComponent>(body);
    }

    public void AfterCultConversion(EntityUid body, EntityUid rule)
    {
        var origin = EnsureComp<AntagRemovalGrantComponent>(body);
        if (!origin.Grants.TryGetValue(RemoveAntagRoles.Cult, out var grant))
            origin.Grants[RemoveAntagRoles.Cult] = grant = new AntagGrantRecord
            {
                RoleId = RemoveAntagRoles.Cult, GameRule = rule
            };

        if (origin.FactionsBeforeCultConversion is { } before)
        {
            var after = GetFactionSnapshot(body);
            if (EntityManager.System<SharedMindSystem>().TryGetMind(body, out var cultMindId, out _))
            {
                if (grant.GrantedMindId is { } previousMind && previousMind != cultMindId)
                    grant.VerifiedProvenance = false;
                else
                {
                    grant.GrantedMindId = cultMindId;
                    grant.VerifiedProvenance = true;
                }
            }

            grant.FactionsAdded.UnionWith(after.Except(before));
            grant.FactionsRemoved.UnionWith(before.Except(after));
        }
    }

    // Track only entities created while a grant scope is active.
    private readonly Stack<RemoveAntagGrantSnapshot> _activeGrantSnapshots = new();

    private void OnGrantedEntityAdded(Entity<MetaDataComponent> entity)
    {
        if (_activeGrantSnapshots.TryPeek(out var current))
            current.CreatedEntities.Add(entity.Owner);
    }

    private bool IsAttachedTo(EntityUid candidate, EntityUid body)
    {
        EntityUid? current = candidate;
        for (var i = 0; i < 64 && current is { } uid; i++)
        {
            if (uid == body)
                return true;

            if (!TryComp(uid, out TransformComponent? transform))
                break;

            var parent = transform.ParentUid;
            if (parent == uid)
                break;

            current = parent;
        }

        return false;
    }
}
