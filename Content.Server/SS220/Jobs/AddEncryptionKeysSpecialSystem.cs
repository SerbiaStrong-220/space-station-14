// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT

using Content.Shared.Radio.Components;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Jobs;

public sealed partial class AddEncryptionKeysSpecialSystem : EntitySystem
{
    [Dependency] private SharedContainerSystem _container = default!;

    public void SetupEncryptionKeys(EntityUid ent, List<EntProtoId> keys, string keySlot)
    {
        if (!HasComp<EncryptionKeyHolderComponent>(ent))
            return;

        var container = _container.EnsureContainer<Container>(ent, keySlot);

        if (container == null)
            return;

        var xform = Transform(ent);

        foreach (var key in keys)
        {
            var item = PredictedSpawnAtPosition(key, xform.Coordinates);
            _container.Insert(item, container);
        }
    }
}
