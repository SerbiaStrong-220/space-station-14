// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Virology.Behaviors;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class VirusInjectionBlockComponent : Component
{
    /// <summary>Localized message shown to whoever trying inject host.</summary>
    [DataField(required: true), AutoNetworkedField]
    public LocId Message;
}
