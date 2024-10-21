// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.StatusEffect;

namespace Content.Shared.SS220.CultYogg.EntitySystems;

public abstract class SharedRaveSystem : EntitySystem
{
    [ValidatePrototypeId<StatusEffectPrototype>]
    public const string EffectKey = "Rave";
}
