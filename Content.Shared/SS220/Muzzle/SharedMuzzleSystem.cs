// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Clothing;
using Content.Shared.Speech.Muting;

namespace Content.Shared.SS220.Muzzle;
public abstract class SharedMuzzleSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MuzzleComponent, ClothingGotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<MuzzleComponent, ClothingGotUnequippedEvent>(OnGotUnequipped);
    }

    private void OnGotUnequipped(EntityUid uid, MuzzleComponent component, ref ClothingGotUnequippedEvent args)
    {
        _entityManager.RemoveComponent<MuzzledComponent>(args.Wearer);
    }

    private void OnGotEquipped(EntityUid uid, MuzzleComponent component, ref ClothingGotEquippedEvent args)
    {
        _entityManager.AddComponent<MuzzledComponent>(args.Wearer);
    }
}
