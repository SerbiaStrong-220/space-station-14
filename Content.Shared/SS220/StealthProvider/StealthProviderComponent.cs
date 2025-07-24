// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;
using Content.Shared.Whitelist;

namespace Content.Shared.SS220.StealthProvider;

[RegisterComponent, NetworkedComponent]
public sealed partial class StealthProviderComponent : Component
{
    [DataField]
    public EntityWhitelist? Whitelist;

    [ViewVariables(VVAccess.ReadWrite)]
    public bool Enabled = false;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public float Range = 3f;

    [ViewVariables(VVAccess.ReadWrite)]
    public List<Entity<ProvidedStealthComponent>> ProvidedEntities = new List<Entity<ProvidedStealthComponent>>();
}
[ByRefEvent]
public record struct StealthProviderStatusChanged(bool Enabled)
{
    public readonly bool Enabled = Enabled;
}
