// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT

using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Jobs;

public sealed partial class AddEncryptionKeysSpecial : JobSpecial
{
    [DataField]
    public string KeySlot = "key_slots";

    [DataField(required: true)]
    public List<EntProtoId> Keys { get; private set; } = new();

    public override void AfterEquip(EntityUid mob)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();
        var keyAdd = entMan.System<AddEncryptionKeysSpecialSystem>();
        keyAdd.SetupEncryptionKeys(mob, Keys, KeySlot);
    }
}
