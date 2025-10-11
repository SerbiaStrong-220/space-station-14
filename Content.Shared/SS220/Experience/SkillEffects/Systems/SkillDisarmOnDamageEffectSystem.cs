// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.SS220.Experience.SkillEffects.Components;
using Content.Shared.SS220.Experience.Systems;
using Robust.Shared.Network;
using Robust.Shared.Random;

namespace Content.Shared.SS220.Experience.SkillEffects.Systems;

public sealed class SkillDisarmOnDamageEffectSystem : EntitySystem
{
    [Dependency] private readonly ExperienceSystem _experience = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly INetManager _netManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        _experience.RelayEventToSkillEntity<DamageChangedEvent>();

        SubscribeLocalEvent<SkillDisarmOnDamageEffectComponent, DamageChangedEvent>(OnDamageChangedEvent);
    }

    public void OnDamageChangedEvent(Entity<SkillDisarmOnDamageEffectComponent> entity, ref DamageChangedEvent args)
    {
        // This waits for predicted random
        if (_netManager.IsClient)
            return;

        if (args.DamageDelta is null)
            return;

        if (DamageSpecifier.GetNegative(args.DamageDelta).GetTotal() < entity.Comp.DamageThreshold)
            return;

        if (!_random.Prob(entity.Comp.DisarmChance))
            return;

        foreach (var hand in _hands.EnumerateHands(entity.Owner))
        {
            _hands.TryDrop(entity.Owner, hand);
        }

        _experience.AddToAdminLogs(entity, "dropped all items in hand", LogImpact.High);
    }
}
