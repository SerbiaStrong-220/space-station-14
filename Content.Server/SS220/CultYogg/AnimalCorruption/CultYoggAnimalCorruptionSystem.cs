// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Mind;
using Content.Shared.SS220.CultYogg.Corruption;
using Robust.Shared.Prototypes;
using System.Diagnostics.CodeAnalysis;

namespace Content.Server.SS220.CultYogg;

public sealed class CultYoggAnimalCorruptionSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    public override void Initialize()
    {
        base.Initialize();
    }
    public void AnimalCorruption(EntityUid uid)//Corrupt animal
    {
        //ToDo Add new animal as the gost role

        if (TerminatingOrDeleted(uid))
            return;

        if (!CheckForCorruption(uid, out var corruptionProto))
            return;

        // Get original body position and spawn MiGo here
        var corruptedAnimal = _entityManager.SpawnAtPosition(corruptionProto.Result, Transform(uid).Coordinates);

        // Move the mind if there is one and it's supposed to be transferred
        if (_mind.TryGetMind(uid, out var mindId, out var mind))
            _mind.TransferTo(mindId, corruptedAnimal, mind: mind);

        //Delete previous entity
        _entityManager.DeleteEntity(uid);
    }
    private bool CheckForCorruption(EntityUid uid, [NotNullWhen(true)] out CultYoggCorruptedAnimalsPrototype? corruption)//if enity_id in list of corruptable
    {
        var idOfEnity = MetaData(uid).EntityPrototype!.ID;

        foreach (var entProto in _prototypeManager.EnumeratePrototypes<CultYoggCorruptedAnimalsPrototype>())//idk if it isn't shitcode
        {
            if (idOfEnity == entProto.ID)
            {
                corruption = entProto;
                return true;
            }
        }
        corruption = null;
        return false;
    }
}
