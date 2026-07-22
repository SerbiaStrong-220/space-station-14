// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT

using Robust.Shared.GameStates;

namespace Content.Shared.SS220.PoweredClothing;

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PoweredClothingComponent : Component
{
    [DataField]
    public float DrawRate = 0f;

    [DataField]
    public TimeSpan DrawTime = TimeSpan.FromSeconds(1f);

    [DataField]
    public bool SelfPowered = true;

    [DataField]
    [AutoNetworkedField]
    public EntityUid PowerSource;
}

[ByRefEvent]
public readonly record struct PoweredClothingTurnedOnEvent()
{
}

[ByRefEvent]
public readonly record struct PoweredClothingTurnedOffEvent()
{
}
