using Content.Server.Administration.Logs;
using Content.Server.Cargo.Systems;
using Content.Server.Storage.Components;
using Content.Shared.Cargo;
using Content.Shared.Database;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Server.SS220.Storage.SpawnOnOpen.Components;
using Content.Shared.Storage.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Content.Shared.Storage.EntitySpawnCollection;

namespace Content.Server.SS220.Storage.SpawnOnOpen.Systems;
internal class SpawnItemsOnOpenSystem
{
    public sealed class SpawnItemsOnUseSystem : EntitySystem
    {
        [Dependency] private readonly SharedTransformSystem _transform = default!;
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SpawnItemsOnOpenComponent, StorageBeforeOpenEvent>(OnOpen);

        }
        private void OnOpen(EntityUid uid, SpawnItemsOnOpenComponent component, StorageBeforeOpenEvent args)
        {
            if (component.Uses <= 0)
                return;

            var coords = Transform(uid).Coordinates;

            foreach (var item in component.Item)
            {
                Spawn(item, coords);
            }

            component.Uses--;
        }
    }
}
