// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;

namespace Content.Shared.SS220.StealthProvider;

[RegisterComponent, NetworkedComponent]
public sealed partial class ProvidedStealthComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public List<Entity<StealthProviderComponent>> StealthProviders = new List<Entity<StealthProviderComponent>>();
}
