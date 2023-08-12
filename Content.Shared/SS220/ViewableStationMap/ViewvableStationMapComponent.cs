using Robust.Shared.Utility;

namespace Content.Shared.SS220.ViewableStationMap;

[RegisterComponent]
public sealed class ViewableStationMapComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    [DataField("mapTexture")]
    public SpriteSpecifier? MapTexture;
}

public enum ViewableStationMapUiKey
{
    Key,
}
