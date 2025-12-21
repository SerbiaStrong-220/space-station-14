using Content.Server.Body.Systems;
using Content.Shared.SS220.HealOnCollide.Bloodstream;

namespace Content.Server.SS220.HealOnCollide.Bloodstream;

public sealed partial class BloodstreamExtensionSystem : SharedBloodstreamExtensionSystem
{
    [Dependency] private readonly BloodstreamSystem _bloodstream = default!;

    public override void TryModifyBleedAmount(EntityUid uid, float amount)
    {
        _bloodstream.TryModifyBleedAmount(uid, amount);
    }
}
