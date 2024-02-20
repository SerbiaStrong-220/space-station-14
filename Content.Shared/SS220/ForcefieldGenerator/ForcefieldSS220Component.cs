// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.ForcefieldGenerator;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ForcefieldSS220Component : Component
{
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public EntityUid? Generator;

    [ViewVariables(VVAccess.ReadOnly)]
    public SoundSpecifier HitSound = new SoundPathSpecifier("/Audio/SS220/Effects/shield/eshild_hit.ogg", new()
    {
        Volume = 1.25f
    });
}
