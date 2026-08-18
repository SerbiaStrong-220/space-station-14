// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Client.SS220.Fork;

/// <summary>
/// Used to change sprite path without changing yaml
///path/to/sprite.rsi -> ForkFolder/path/to/sprite.rsi
/// </summary>
[RegisterComponent]
public sealed partial class ForkSpriteComponent : Component
{
    [DataField]
    public bool Disabled = false;

    public const string ForkFolder = "SS220";
}
