// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.SS220.TraitorDynamics;

/// <summary>
/// Shared part of TraitorDynamics
/// </summary>
public abstract class SharedTraitorDynamicsSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    [ValidatePrototypeId<WeightedRandomPrototype>]
    private const string WeightsProto = "WeightedDynamicsList";

    protected ProtoId<DynamicPrototype>? CurrentDynamic = null; // prob we should have nullspace entity with comp

    /// <summary>
    /// Gets a random DynamicPrototype from WeightedRandomPrototype, weeding out unsuitable dynamics
    /// </summary>
    /// <param name="playerCount"> current number of ready players, by this indicator the required number is compared </param>
    /// <param name="force"> ignore player checks and force any dynamics </param>
    /// <returns></returns>
    public string GetRandomDynamic(int playerCount = default, bool force = false)
    {
        var validWeight = _prototype.Index<WeightedRandomPrototype>(WeightsProto);
        var tempWeight = validWeight;
        var selectedDynamic = string.Empty;

        while (tempWeight.Weights.Keys.Count > 0)
        {
            var currentDynamic = tempWeight.Pick(_random);

            if (!_prototype.TryIndex<DynamicPrototype>(currentDynamic, out var dynamicProto))
            {
                tempWeight.Weights.Remove(currentDynamic);
                continue;
            }

            if (playerCount == default || force)
            {
                selectedDynamic = dynamicProto.ID;
                break;
            }

            if (TrySelectDynamic(currentDynamic, dynamicProto, playerCount, out selectedDynamic))
                break;

            tempWeight.Weights.Remove(currentDynamic);
        }

        return selectedDynamic;
    }

    private bool TrySelectDynamic(string currentDynamic, DynamicPrototype dynamicProto, int playerCount, out string selectedDynamic)
    {
        selectedDynamic = string.Empty;

        if (playerCount < dynamicProto.PlayersRequerment)
            return false;

        selectedDynamic = currentDynamic;
        return true;

    }


    /// <summary>
    /// Tries to find the type of dynamic while in Traitor game rule
    /// </summary>
    /// <returns>installed dynamic</returns>
    public ProtoId<DynamicPrototype>? GetCurrentDynamic()
    {
        return CurrentDynamic;
    }

    public sealed class DynamicAddedEvent : EntityEventArgs
    {
        public ProtoId<DynamicPrototype> Dynamic;

        public DynamicAddedEvent(ProtoId<DynamicPrototype> dynamic)
        {
            Dynamic = dynamic;
        }
    }

    public sealed class DynamicSetAttempt : CancellableEntityEventArgs
    {
        public ProtoId<DynamicPrototype> Dynamic;

        public DynamicSetAttempt(ProtoId<DynamicPrototype> dynamic)
        {
            Dynamic = dynamic;
        }
    }
}
