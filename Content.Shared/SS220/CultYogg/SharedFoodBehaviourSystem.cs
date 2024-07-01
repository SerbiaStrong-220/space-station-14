// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Nutrition;
using Content.Shared.Humanoid;
using Robust.Shared.Prototypes;
using Content.Shared.Mind;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Network;
using Content.Shared.StatusEffect;
using Content.Shared.Drunk;//Delete this after

namespace Content.Shared.SS220.CultYogg;

public abstract class SharedFoodBehaviourSystem : EntitySystem
{
    [ValidatePrototypeId<StatusEffectPrototype>]
    public const string DrunkKey = "Drunk";

    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FoodBehaviourComponent, ConsumeDoAfterEvent>(OnDoAfter);
    }
    private void OnDoAfter(Entity<FoodBehaviourComponent> entity, ref ConsumeDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target == null)
            return;

        args.Handled = true;

        if (!HasComp<HumanoidAppearanceComponent>(args.Target))
        {
            AnimalCorruption((EntityUid) args.Target);//beast must be transformed
            return;
        }
        if (!_entityManager.TryGetComponent<CultYoggComponent>(args.Target, out var comp))
        {
            //figure out function to increase amount of consumed shrooms
            return;
        }

        ShroomEffect((EntityUid) args.Target);
    }

    private void ShroomEffect(EntityUid uid, StatusEffectsComponent? status = null)//Add special effects if consumed
    {
        if (!Resolve(uid, ref status, false))
            return;

       // if (TryComp<LightweightDrunkComponent>(uid, out var trait))
      //      boozePower *= trait.BoozeStrengthMultiplier;

        if (!_statusEffectsSystem.HasStatusEffect(uid, DrunkKey, status))
        {
            _statusEffectsSystem.TryAddStatusEffect<DrunkComponent>(uid, DrunkKey, TimeSpan.FromSeconds(30), true, status);
        }
        else
        {
            _statusEffectsSystem.TryAddTime(uid, DrunkKey, TimeSpan.FromSeconds(30), status);
        }
    }
    private void AnimalCorruption(EntityUid uid)//Corrupt animal
    {
        //ToDo AddGhost role
        if (_net.IsClient)
            return;

        if (TerminatingOrDeleted(uid))
            return;

        if (!CheckForCorruption(uid, out var corruptionProto))
        {
            //maybe do smth if its isn't in list
            return;
        }

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
