using System.Numerics;

namespace Content.Server.SS220.DarkForces.Saint.Chaplain.Abilities;

public struct Box2EdgeEnumerator
{
    private readonly bool _corners;
    private readonly Box2 _box;
    private float _x;
    private float _y;

    public Box2EdgeEnumerator(Box2 box, bool corners)
    {
        _box = box;
        _corners = corners;
        _x = _box.Left;
        _y = _box.Bottom;
    }

    public bool MoveNext(out Vector2 index)
    {
        for (var x = _x; x < _box.Right; x++)
        {
            for (var y = _y; y < _box.Top; y++)
            {
                if (x != _box.Left &&
                    x != _box.Right + -1 &&
                    y != _box.Bottom &&
                    y != _box.Top + -1)
                {
                    continue;
                }

                if (!_corners &&
                    (x == _box.Left && (y == _box.Bottom || y == _box.Top + -1) ||
                    x == _box.Right && (y == _box.Bottom || y == _box.Top + -1)))
                {
                    continue;
                }

                _x = x;
                _y = y + 1;

                if (_y == _box.Top)
                {
                    _x++;
                    _y = _box.Bottom;
                }

                index = new Vector2(x, y);
                return true;
            }
        }

        index = default;
        return false;
    }
}
