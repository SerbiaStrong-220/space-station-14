// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.EntityEffects.Effects;

/// <summary>
/// This is used for displaying grilling smoke
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GrillingVisualComponent : Component
{
    [DataField, AutoNetworkedField]
    public SpriteSpecifier.Rsi GrillingSprite = new(new("SS220/Effects/grillingEffect.rsi"), "grillingSmoke");

    [DataField, AutoNetworkedField]
    public string GrillingLayer = "grilling-layer";
}
