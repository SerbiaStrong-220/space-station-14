// © FCB, MIT, full text: https://github.com/Free-code-base-14/space-station-14/blob/master/LICENSE.TXT
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.ArmorBlock;

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]

public sealed partial class ArmorBlockComponent : Component
{
    /// <summary>
    /// The entity this armor protects(must be set manually in every implementation, made for reusability)
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? User = null;
}
