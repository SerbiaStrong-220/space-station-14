// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Numerics;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Explosion.EntitySystems;
using Content.Shared.Actions;
using Content.Shared.Chat;
using Content.Shared.Inventory;
using Content.Shared.SS220.Virology.Behaviors;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;

namespace Content.Server.SS220.Virology.Behaviors;

public sealed partial class FireBreathingSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedGunSystem _gun = default!;
    [Dependency] private FlammableSystem _flammable = default!;
    [Dependency] private SharedChatSystem _chat = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private ExplosionSystem _explosion = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FireBreathingComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<FireBreathingComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<FireBreathingComponent, FireBreathingActionEvent>(OnFireBreath);
    }

    private void OnStartup(Entity<FireBreathingComponent> ent, ref ComponentStartup args)
    {
        _actions.AddAction(ent.Owner, ref ent.Comp.ActionEntity, ent.Comp.ActionId);
        _actions.SetUseDelay(ent.Comp.ActionEntity, ent.Comp.Cooldown);
    }

    private void OnShutdown(Entity<FireBreathingComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Owner, ent.Comp.ActionEntity);
    }

    private void OnFireBreath(Entity<FireBreathingComponent> ent, ref FireBreathingActionEvent args)
    {
        if (args.Handled)
            return;

        var origin = _transform.GetWorldPosition(ent);
        var target = _transform.ToMapCoordinates(args.Target).Position;

        var dir = target - origin;
        if (dir.LengthSquared() < ent.Comp.AimDeadzoneSq)
            return;

        args.Handled = true;

        _chat.TryEmoteWithChat(ent, ent.Comp.SneezeEmote);
        _audio.PlayPvs(ent.Comp.Sound, ent);

        var dirNorm = Vector2.Normalize(dir);
        var spawnPos = new MapCoordinates(origin + dirNorm * ent.Comp.SpawnOffset, Transform(ent).MapID);
        var fireball = Spawn(ent.Comp.FireballProto, spawnPos);

        // helmet (hardsuit) catches fireball so it detonates instead of flying out
        if (_inventory.TryGetSlotEntity(ent, "head", out var head) && HasComp<FireBreathSealComponent>(head))
            _explosion.TriggerExplosive(fireball);
        else
            _gun.ShootProjectile(fireball, dirNorm, Vector2.Zero, ent, ent, ent.Comp.FireballSpeed);

        _flammable.AdjustFireStacks(ent, ent.Comp.SelfFireStacks, ignite: true);
    }
}
