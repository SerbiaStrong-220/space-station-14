// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Pathology;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PathologyInjectionBlockComponent : Component
{
    /// <summary>Localized message shown to whoever tries to inject host.</summary>
    [DataField(required: true), AutoNetworkedField]
    public LocId Message;
}
