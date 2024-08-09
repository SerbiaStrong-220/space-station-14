// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.CultYogg;
using Robust.Shared.Prototypes;
using Content.Shared.Nutrition;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Network;
using Content.Shared.StatusEffect;
using Content.Shared.Drunk;//Delete this after
namespace Content.Server.SS220.CultYogg;

public sealed class CultYoggAnimalCorruptionSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;
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
        //var idOfEnity = _entityManager.GetComponent<MetaDataComponent>(uid).EntityPrototype!.ID;

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
