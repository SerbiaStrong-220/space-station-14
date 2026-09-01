using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.AirDefense;

[RegisterComponent]
[NetworkedComponent]
public sealed partial class AirDefenseComponent : Component
{
    [DataField]
    public float MissProbability = 0.25f;

    [DataField]
    public EntityWhitelist Whitelist = new()
    {
        Tags = new()
        {
            "AirDefenseTarget",
        },
    };
}
