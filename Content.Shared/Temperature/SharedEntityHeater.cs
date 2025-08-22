using Robust.Shared.Serialization;

namespace Content.Shared.Temperature;

[Serializable, NetSerializable]
public enum EntityHeaterVisuals
{
    Setting
}

/// <summary>
/// What heat the heater is set to, if on at all.
/// </summary>
[Serializable, NetSerializable]
public enum EntityHeaterSetting
{
    Off,
    Low,
    Medium,
    High
}

//SS220-grill-visuals begin
[Serializable, NetSerializable]
public enum FoodCookingVisuals
{
    Grilling
}
//SS220-grill-visuals end
