// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.ModuleFurniture;

public static class TiledLayoutHelpers
{
    public static int TiledLayoutVector2iComparer(Vector2i one, Vector2i other)
    {
        int oneHeight = one.Y;
        int oneWidth = one.X;
        int otherHeight = other.Y;
        int otherWidth = other.X;

        if (oneHeight == otherHeight)
            return oneWidth.CompareTo(otherWidth);

        return oneHeight.CompareTo(otherHeight);
    }
}
