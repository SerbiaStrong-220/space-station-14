// using System.Numerics;
// using Content.Shared.SecretStation.DarkForces.Ratvar.Righteous.Roles;
// using Robust.Client.GameObjects;
// using Robust.Shared.GameObjects;
// using Robust.Shared.IoC;
// using Robust.Shared.Random;
// using Robust.Shared.Utility;
//
// namespace Content.SecretStationClient.DarkForces.Ratvar.Roles;
//
// public sealed class RatvarIconsSystem : EntitySystem
// {
//     [Dependency] private readonly IRobustRandom _random = default!;
//
//     public override void Initialize()
//     {
//         base.Initialize();
//         SubscribeLocalEvent<RatvarRighteousComponent, ComponentStartup>(RighteousIconAdd);
//         SubscribeLocalEvent<RatvarRighteousComponent, ComponentShutdown>(RighteousIconRemove);
//     }
//
//     private void RighteousIconAdd(EntityUid uid, RatvarRighteousComponent component, ComponentStartup args)
//     {
//         AddIcon(uid);
//     }
//
//     private void RighteousIconRemove(EntityUid uid, RatvarRighteousComponent component, ComponentShutdown args)
//     {
//         RemoveIcon(uid);
//     }
//
//     private void AddIcon(EntityUid uid)
//     {
//         if (!TryComp<SpriteComponent>(uid, out var sprite))
//             return;
//
//         if (sprite.LayerMapTryGet(RatvarIconKey.Key, out _))
//             return;
//
//         var adj = sprite.Bounds.Height / 2 + 1.0f/32 * 10.0f;
//
//         var randomIndex = _random.Next(0, States.Length);
//
//         var randomState = States[randomIndex];
//
//         var layer = sprite.AddLayer(new SpriteSpecifier.Rsi(new ResPath(Rsi), randomState));
//
//         sprite.LayerMapSet(RatvarIconKey.Key, layer);
//         sprite.LayerSetOffset(layer, new Vector2(0.0f, adj));
//     }
//
//     private void RemoveIcon(EntityUid uid)
//     {
//         if (!TryComp<SpriteComponent>(uid, out var sprite))
//             return;
//
//         if (!sprite.LayerMapTryGet(RatvarIconKey.Key, out var layer))
//             return;
//
//         sprite.RemoveLayer(layer);
//     }
//
//     private enum RatvarIconKey
//     {
//         Key
//     }
// }
