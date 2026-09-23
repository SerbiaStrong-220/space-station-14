// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Lock;

namespace Content.Shared.Radio.EntitySystems;

/// <summary>
///     This system manages encryption keys & key holders for use with radio channels.
/// </summary>
public sealed partial class EncryptionKeySystem : EntitySystem
{
    private bool IsLocked(EntityUid uid)
    {
        return TryComp<LockComponent>(uid, out var lockComp) && lockComp.Locked;
    }
}
