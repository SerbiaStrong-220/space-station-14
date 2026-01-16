using Content.Shared.CrewManifest;
using Content.Shared.PDA;
using Content.Shared.Roles;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.CrewManifest.UI;

public sealed class CrewManifestListing : BoxContainer
{
    [Dependency] private readonly IEntitySystemManager _entitySystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    // ss220 add additional info for pda start
    [Dependency] private readonly IEntityManager _entMan = default!;
    // ss220 add additional info for pda end

    private readonly SpriteSystem _spriteSystem;

    public CrewManifestListing()
    {
        IoCManager.InjectDependencies(this);
        _spriteSystem = _entitySystem.GetEntitySystem<SpriteSystem>();
    }

    public void AddCrewManifestEntries(CrewManifestEntries entries, Entity<PdaComponent>? pda = null) // ss220 add additional info for pda
    {
        var entryDict = new Dictionary<DepartmentPrototype, List<CrewManifestEntry>>();
        var cryoList = new List<CrewManifestEntry>(); // SS220 Cryo-Manifest

        foreach (var entry in entries.Entries)
        {
            // SS220 Cryo-Manifest begin
            if (entry.IsInCryo)
            {
                cryoList.Add(entry);
                continue;
            }
            // SS220 Cryo-Manifest end

            foreach (var department in _prototypeManager.EnumeratePrototypes<DepartmentPrototype>())
            {
                // this is a little expensive, and could be better
                if (department.Roles.Contains(entry.JobPrototype))
                {
                    entryDict.GetOrNew(department).Add(entry);
                }
            }
        }

        var entryList = new List<(DepartmentPrototype section, List<CrewManifestEntry> entries)>();

        foreach (var (section, listing) in entryDict)
        {
            entryList.Add((section, listing));
        }

        entryList.Sort((a, b) => DepartmentUIComparer.Instance.Compare(a.section, b.section));

        foreach (var item in entryList)
        {
            // ss220 add additional info for pda start
            AddChild(new CrewManifestSection(_entMan, _prototypeManager, _spriteSystem, item.section, item.entries, pda));
            // ss220 add additional info for pda end
        }

        // SS220 Cryo-Manifest
        if (cryoList.Count > 0)
        {
            if (_prototypeManager.TryIndex<DepartmentPrototype>("Cryo", out var cryoDepartment))
                AddChild(new CrewManifestSection(_entMan, _prototypeManager, _spriteSystem, cryoDepartment, cryoList, pda)); // ss220 add additional info for pda
        }
    }
}
