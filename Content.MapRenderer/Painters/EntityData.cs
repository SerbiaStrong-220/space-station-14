using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.MapRenderer.Painters;

public readonly record struct EntityData(EntityUid Owner, SpriteComponent Sprite, float X, float Y)
{
    public readonly EntityUid Owner = Owner;

    public readonly SpriteComponent Sprite = Sprite;

    public readonly float X = X;

    public readonly float Y = Y;

    public readonly MetaDataComponent MetaData { get; init; } // SS220 Map Rendering Crash Fix
    public readonly System.Numerics.Vector2 LocalPosition { get; init; } // SS220 Map Rendering Crash Fix
}
