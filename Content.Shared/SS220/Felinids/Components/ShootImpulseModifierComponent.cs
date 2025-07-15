// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Felinids.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ShootImpulseComponent : Component
{
    /// <summary>
    ///     After firing recoil modifier.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ImpulseModifier = 1f; // TODO: Установить как 4 в прототипе фелинида

    /// <summary>
    ///     Add recoil in gravity?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool RecoilOnGround = false; // TODO: Активировать в прототипе фелинида
}
