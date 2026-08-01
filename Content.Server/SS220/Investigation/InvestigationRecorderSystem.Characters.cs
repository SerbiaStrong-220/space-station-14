// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Linq;
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

    private object BuildCharacterSnapshot(EntityUid uid, EntityUid? mindId, out int fingerprint)
    {
        string? species = null;
        string? gender = null;
        var age = 0;

        if (TryComp<HumanoidProfileComponent>(uid, out var profile))
        {
            // Copied to locals: the component is [Access]-restricted, and direct calls read as execute access.
            var speciesProto = profile.Species;
            var profileGender = profile.Gender;

            species = speciesProto.Id;
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

        var access = _accessReader.FindAccessTags(uid)
            .Select(tag => tag.Id)
            .OrderBy(tag => tag, StringComparer.Ordinal)
            .ToList();

        var worn = new Dictionary<string, string>();
        if (TryComp<InventoryComponent>(uid, out var inventory))
        {
            var slots = _inventory.GetSlotEnumerator((uid, inventory));
            while (slots.MoveNext(out var container))
            {
                if (container.ContainedEntity is not { } item)
                    continue;

                worn[container.ID] = DescribeItem(item);
            }
        }

        var held = _hands.EnumerateHeld(uid)
            .Select(DescribeItem)
            .ToList();

        var carried = new List<string>();
        if (_storageDepth > 0)
        {
            if (inventory != null)
            {
                var slots = _inventory.GetSlotEnumerator((uid, inventory));
                while (slots.MoveNext(out var container))
                {
                    if (container.ContainedEntity is { } item)
                        CollectStorage(item, _storageDepth, carried);
                }
            }

            foreach (var item in _hands.EnumerateHeld(uid))
            {
                CollectStorage(item, _storageDepth, carried);
            }

            carried.Sort(StringComparer.Ordinal);
        }

        var name = _metaQuery.TryComp(uid, out var meta) ? meta.EntityName : null;

        fingerprint = ComputeFingerprint(species, job, name, antagRoles, access, worn, held, carried);

        return new
        {
            t = _timing.CurTick.Value,
            e = uid.Id,
            name,
            species,
            gender,
            age,
            job,
            department,
            antag = antagRoles is { Count: > 0 } ? true : (bool?) null,
            roles = antagRoles is { Count: > 0 } ? antagRoles : null,
            access,
            worn,
            hands = held,
            carried,
        };
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

    private readonly record struct AntagRole(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("name")] string Name);

    private string DescribeItem(EntityUid item)
    {
        if (!_metaQuery.TryComp(item, out var meta))
            return "<unknown>";

        return meta.EntityPrototype?.ID ?? meta.EntityName;
    }

    private static int ComputeFingerprint(
        string? species,
        string? job,
        string? name,
        List<AntagRole>? antagRoles,
        List<string> access,
        Dictionary<string, string> worn,
        List<string> held,
        List<string> carried)
    {
        var hash = new HashCode();
        hash.Add(species);
        hash.Add(job);
        hash.Add(name);

        if (antagRoles != null)
        {
            foreach (var role in antagRoles)
            {
                hash.Add(role.Id);
            }
        }

        foreach (var tag in access)
            hash.Add(tag);

        foreach (var (slot, item) in worn)
        {
            hash.Add(slot);
            hash.Add(item);
        }

        foreach (var item in held)
            hash.Add(item);

        foreach (var item in carried)
            hash.Add(item);

        return hash.ToHashCode();
    }
}
