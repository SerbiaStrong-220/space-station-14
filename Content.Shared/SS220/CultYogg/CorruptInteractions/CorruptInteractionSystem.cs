// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.SS220.CultYogg.Cultists;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;

namespace Content.Shared.SS220.CultYogg.CorruptInteractions;

public sealed class CorruptInteractionsSystem : EntitySystem
{
    [Dependency] private readonly WeldableSystem _weldable = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WeldCorruptInteractionComponent, CorruptInteractionEvent>(OnWeldCorruptInteraction);
        SubscribeLocalEvent<DamageCorruptInteractionComponent, CorruptInteractionEvent>(OnDamageCorruptInteraction);
    }

    private void OnWeldCorruptInteraction(Entity<WeldCorruptInteractionComponent> ent, ref CorruptInteractionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<WeldableComponent>(ent, out var weldable))
            return;

        _weldable.SetWeldedState(ent, !weldable.IsWelded);

        args.Handled = true;
    }

    private void OnDamageCorruptInteraction(Entity<DamageCorruptInteractionComponent> ent, ref CorruptInteractionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<DamageableComponent>(ent, out var damageableComp))
            return;

        _damageable.TryChangeDamage(ent, ent.Comp.Damage, true, interruptsDoAfters: false, damageableComp);

        args.Handled = true;
    }
}
