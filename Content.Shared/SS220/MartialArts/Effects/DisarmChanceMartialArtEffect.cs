// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.CombatMode;
using Content.Shared.Damage.Systems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.MartialArts.Effects;

public sealed partial class DisarmChanceMartialArtEffectSystem : BaseMartialArtEffectSystem<DisarmChanceMartialArtEffect, DisarmChanceMartialArtEffectComponent>
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DisarmChanceMartialArtEffectComponent, DisarmChanceModifierEvent>(OnDisarmChanceModifier);

        // if we applied our effect we will mark DisarmedEvent as handled and hands system wont do anything
        // if we haven't did anything the other systems should take care about it
        // hacky but do we have other way?
        SubscribeLocalEvent<MartialArtsTargetComponent, DisarmedEvent>(OnDisarm, before: [typeof(SharedHandsSystem), typeof(SharedStaminaSystem)]);
    }

    private void OnDisarmChanceModifier(EntityUid user, DisarmChanceMartialArtEffectComponent comp, DisarmChanceModifierEvent ev)
    {
        if (!TryEffect(user, out var effect))
            return;

        ev.BaseChance = effect.Chance;
    }

    private void OnDisarm(EntityUid target, MartialArtsTargetComponent comp, ref DisarmedEvent ev)
    {
        if (ev.Handled)
            return;

        var user = ev.Source;

        if (!TryEffect(user, out var effect))
            return;

        if (effect.ToHand)
        {
            var held = _hands.EnumerateHeld(target);

            if (held.TryFirstOrNull(out var item))
            {
                _hands.PickupOrDrop(user, item.Value);

                ev.Handled = true;
                ev.PopupPrefix = "martial-art-effects-disarm-success-";
            }
        }
    }
}

public sealed partial class DisarmChanceMartialArtEffect : MartialArtEffectBase<DisarmChanceMartialArtEffect>
{
    [DataField(required: true)]
    public float Chance;

    [DataField]
    public bool ToHand = true;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class DisarmChanceMartialArtEffectComponent : Component;
