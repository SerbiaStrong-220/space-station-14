// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.RandomSpriteFixture;

/// <summary>
/// Resizes a fixture to match the state picked by RandomSpriteComponent.
/// </summary>
[RegisterComponent]
public sealed partial class RandomSpriteFixtureComponent : Component
{
    [DataField]
    public string FixtureId = "fix1";

    /// <summary>
    /// Fixture bounds by sprite state.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<string, Box2> Bounds = new();
}
