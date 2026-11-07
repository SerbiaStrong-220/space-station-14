// using Content.Server.SS220.DarkForces.Saint.Saintable;
// using Content.Server.Bible;
// using Content.Server.SS220.Bridges;
// using Robust.Shared.GameObjects;
// using Robust.Shared.IoC;

// namespace Content.Server.SS220.DarkForces.Saint.Items;

// public sealed class SaintedBridge : ISaintedBridge
// {
//     [Dependency] private readonly IEntityManager _entityManager = default!;

//     public bool TryMakeSainted(EntityUid user, EntityUid uid)
//     {
//         return _entityManager.System<SaintedSystem>().TryMakeSainted(user, uid);
//     }
// }
