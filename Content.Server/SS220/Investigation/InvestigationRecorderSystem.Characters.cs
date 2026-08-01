// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Text.Json.Serialization;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Shared.Storage;

namespace Content.Server.SS220.Investigation;

public sealed partial class InvestigationRecorderSystem
{
    private Dictionary<string, string> DepartmentsByJob
    {
        get
        {
            if (_departmentsByJob is { } cached)
                return cached;

            var byJob = new Dictionary<string, string>();
            foreach (var department in _prototypes.EnumeratePrototypes<DepartmentPrototype>())
            {
                foreach (var role in department.Roles)
                {
                    byJob.TryAdd(role, department.ID);
                }
            }

            _departmentsByJob = byJob;
            return byJob;
        }
    }

    private void BeginCharacterSweep()
    {
        // Refilling mid-sweep would drop the tail it never reached and restart from the top, forever.
        if (_sweepQueue.Count > 0)
            return;

        var query = EntityQueryEnumerator<InvestigationTrackedComponent>();
        while (query.MoveNext(out var uid, out var tracked))
        {
            _sweepQueue.Enqueue(uid);
            tracked.DirtyLoadout = false;
        }
    }

    private void AdvanceCharacterSweep()
    {
        for (var sampled = 0; sampled < SweepBatchSize && _sweepQueue.TryDequeue(out var uid); sampled++)
        {
            if (_trackedQuery.TryComp(uid, out var tracked))
                SampleCharacter((uid, tracked));
        }
    }

    private void DrainDirtyCharacters()
    {
        var query = EntityQueryEnumerator<InvestigationTrackedComponent>();
        while (query.MoveNext(out var uid, out var tracked))
        {
            if (!tracked.DirtyLoadout)
                continue;

            tracked.DirtyLoadout = false;
            SampleCharacter((uid, tracked));
        }
    }

    private void SampleCharacter(Entity<InvestigationTrackedComponent> ent)
    {
        var uid = ent.Owner;
        EntityUid? mindId = _mind.TryGetMind(uid, out var mind, out _) ? mind : null;

        var snapshot = BuildCharacterSnapshot(uid, mindId, out var fingerprint);
        if (ent.Comp.LoadoutHash != fingerprint)
        {
            ent.Comp.LoadoutHash = fingerprint;
            _recorder.WriteCharacterRow(snapshot);
        }

        if (mindId is { } ownedMind)
            SampleObjectives(uid, ownedMind);
    }

    private void SampleObjectives(EntityUid owner, EntityUid mindId)
    {
        if (!TryComp<MindComponent>(mindId, out var mind) || mind.Objectives.Count == 0)
            return;

        foreach (var objective in mind.Objectives)
        {
            if (_objectives.GetInfo(objective, mindId, mind) is not { } info)
                continue;

            var prototype = _metaQuery.TryComp(objective, out var meta) ? meta.EntityPrototype?.ID : null;

            _recorder.WriteObjectiveIfChanged(
                objective,
                owner,
                prototype,
                info.Title,
                info.Description,
                info.Progress);
        }
    }

    private void MarkCharacterDirty(EntityUid uid)
    {
        if (_trackedQuery.TryComp(uid, out var tracked))
            tracked.DirtyLoadout = true;
    }

    private CharacterSnapshot BuildCharacterSnapshot(EntityUid uid, EntityUid? mindId, out int fingerprint)
    {
        string? species = null;
        string? gender = null;
        var age = 0;

        if (TryComp<HumanoidProfileComponent>(uid, out var profile))
        {
            var profileGender = profile.Gender;

            species = profile.Species.Id;
            gender = profileGender.ToString();
            age = profile.Age;
        }

        string? job = null;
        List<AntagRole>? antagRoles = null;

        if (mindId is { } mind)
        {
            if (_jobs.MindTryGetJobId(mind, out var jobProto))
                job = jobProto?.Id;

            antagRoles = BuildAntagRoles(mind);
        }

        string? department = null;
        if (job != null && DepartmentsByJob.TryGetValue(job, out var jobDepartment))
            department = jobDepartment;

        var access = new List<string>();
        foreach (var tag in _accessReader.FindAccessTags(uid))
        {
            access.Add(tag.Id);
        }

        access.Sort(StringComparer.Ordinal);

        var worn = new Dictionary<string, string>();
        var held = new List<string>();
        var carried = new List<string>();

        if (TryComp<InventoryComponent>(uid, out var inventory))
        {
            var slots = _inventory.GetSlotEnumerator((uid, inventory));
            while (slots.MoveNext(out var container))
            {
                if (container.ContainedEntity is not { } item)
                    continue;

                worn[container.ID] = DescribeItem(item);
                CollectStorage(item, _storageDepth, carried);
            }
        }

        foreach (var item in _hands.EnumerateHeld(uid))
        {
            held.Add(DescribeItem(item));
            CollectStorage(item, _storageDepth, carried);
        }

        carried.Sort(StringComparer.Ordinal);

        var snapshot = new CharacterSnapshot(
            _timing.CurTick.Value,
            uid.Id,
            _metaQuery.TryComp(uid, out var meta) ? meta.EntityName : null,
            species,
            gender,
            age,
            job,
            department,
            antagRoles is { Count: > 0 } ? true : null,
            antagRoles is { Count: > 0 } ? antagRoles : null,
            access,
            worn,
            held,
            carried);

        fingerprint = ComputeFingerprint(snapshot);
        return snapshot;
    }

    private List<AntagRole>? BuildAntagRoles(EntityUid mindId)
    {
        List<AntagRole>? antagRoles = null;

        foreach (var role in _roles.MindGetAllRoleInfo(mindId))
        {
            if (!role.Antagonist)
                continue;

            antagRoles ??= new List<AntagRole>();
            antagRoles.Add(new AntagRole(role.Prototype, Loc.GetString(role.Name)));
        }

        return antagRoles;
    }

    private void CollectStorage(EntityUid container, int depth, List<string> into)
    {
        if (depth <= 0 || !TryComp<StorageComponent>(container, out var storage))
            return;

        foreach (var item in storage.Container.ContainedEntities)
        {
            into.Add(DescribeItem(item));
            CollectStorage(item, depth - 1, into);
        }
    }

    private string DescribeItem(EntityUid item)
    {
        if (!_metaQuery.TryComp(item, out var meta))
            return "<unknown>";

        return meta.EntityPrototype?.ID ?? meta.EntityName;
    }

    /// <remarks>Deliberately skips tick, entity, gender, age and department: none of them can change alone.</remarks>
    private static int ComputeFingerprint(in CharacterSnapshot snapshot)
    {
        var hash = new HashCode();
        hash.Add(snapshot.Species);
        hash.Add(snapshot.Job);
        hash.Add(snapshot.Name);

        if (snapshot.Roles != null)
        {
            foreach (var role in snapshot.Roles)
            {
                hash.Add(role.Id);
            }
        }

        foreach (var tag in snapshot.Access)
            hash.Add(tag);

        foreach (var (slot, item) in snapshot.Worn)
        {
            hash.Add(slot);
            hash.Add(item);
        }

        foreach (var item in snapshot.Hands)
            hash.Add(item);

        foreach (var item in snapshot.Carried)
            hash.Add(item);

        return hash.ToHashCode();
    }
}

public readonly record struct AntagRole(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name);

/// <remarks>Serialized as one <c>characters</c> row; property names are the on-disk field names (§4.6).</remarks>
public readonly record struct CharacterSnapshot(
    [property: JsonPropertyName("t")] uint Tick,
    [property: JsonPropertyName("e")] int Entity,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("species")] string? Species,
    [property: JsonPropertyName("gender")] string? Gender,
    [property: JsonPropertyName("age")] int Age,
    [property: JsonPropertyName("job")] string? Job,
    [property: JsonPropertyName("department")] string? Department,
    [property: JsonPropertyName("antag")] bool? Antag,
    [property: JsonPropertyName("roles")] List<AntagRole>? Roles,
    [property: JsonPropertyName("access")] List<string> Access,
    [property: JsonPropertyName("worn")] Dictionary<string, string> Worn,
    [property: JsonPropertyName("hands")] List<string> Hands,
    [property: JsonPropertyName("carried")] List<string> Carried);
