using Robust.Shared.Utility;

namespace Content.Shared.Atmos.Components;

[RegisterComponent]
public sealed partial class PipeAppearanceComponent : Component
{
    [DataField]
    public SpriteSpecifier.Rsi[] Sprite = [new(new("SS220/Structures/Piping/Atmospherics/pipe.rsi"), "pipeConnector"), // SS220-resprite
        new(new("SS220/Structures/Piping/Atmospherics/pipe_alt1.rsi"), "pipeConnector"), // SS220 atmospherics-sprite-update
        new(new("SS220/Structures/Piping/Atmospherics/pipe_alt2.rsi"), "pipeConnector")]; // SS220 atmospherics-sprite-update
}
