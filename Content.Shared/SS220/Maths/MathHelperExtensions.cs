// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;

namespace Content.Shared.SS220.Maths;

public static partial class MathHelperExtensions
{
    /// <summary>
    /// Substracts the <paramref name="cutters"/> from the <paramref name="boxes"/> returning the remaining sections
    /// </summary>
    public static List<Box2> SubstructBoxes(IEnumerable<Box2> boxes, IEnumerable<Box2> cutters, bool unionResult = true)
    {
        var result = boxes.ToList();
        foreach (var cutter in cutters)
            result = SubstructBox(result, cutter);

        if (unionResult)
            result = UnionInEqualSizedBoxes(result);

        return result;
    }

    /// <summary>
    /// Substracts the <paramref name="cutter"/> from the <paramref name="boxes"/> returning the remaining sections
    /// </summary>
    public static List<Box2> SubstructBox(IEnumerable<Box2> boxes, Box2 cutter, bool unionResult = true)
    {
        var result = new List<Box2>();
        foreach (var box in boxes)
            result.AddRange(SubstructBox(box, cutter));

        if (unionResult)
            result = UnionInEqualSizedBoxes(result);

        return result;
    }

    /// <summary>
    /// Substracts the <paramref name="cutter"/> from the <paramref name="box"/> returning the remaining sections
    /// </summary>
    public static List<Box2> SubstructBox(Box2 box, Box2 cutter)
    {
        var result = new List<Box2>();

        var inter = box.Intersect(cutter);
        if (Box2.Area(inter) <= 0)
        {
            result.Add(box);
            return result;
        }

        if (inter.Top < box.Top)
            result.Add(new Box2(box.Left, inter.Top, box.Right, box.Top));

        if (inter.Bottom > box.Bottom)
            result.Add(new Box2(box.Left, box.Bottom, box.Right, inter.Bottom));

        if (inter.Left > box.Left)
            result.Add(new Box2(box.Left, inter.Bottom, inter.Left, inter.Top));

        if (inter.Right < box.Right)
            result.Add(new Box2(inter.Right, inter.Bottom, box.Right, inter.Top));

        return result;
    }

    /// <summary>
    /// Returns a new array of boxes in which all intersections in <paramref name="boxes"/> has removed
    /// </summary>
    public static List<Box2> GetNonOverlappingBoxes(IEnumerable<Box2> boxes)
    {
        var result = new List<Box2>();

        foreach (var box in boxes)
        {
            // Названо parts т.к. в случае пересечений с другими боксами - текущий бокс дробится на непересекающиеся части
            var parts = new List<Box2> { box };
            foreach (var exist in result)
            {
                var newParts = new List<Box2>();
                foreach (var part in parts)
                {
                    if (part.IntersectPercentage(exist) > 0)
                    {
                        // Если текущий бокс пересекается с другими боксами - то вырезает его и продолжает проверки с его остатками
                        var subParts = SubstructBox(part, exist);
                        newParts.AddRange(subParts);
                    }
                    else
                    {
                        // Если текущий бокс не пересекается с другими боксами - то продолжает проверку с ним
                        newParts.Add(part);
                    }
                }

                parts = newParts;
            }

            result.AddRange(parts);
        }

        return result;
    }

    /// <summary>
    /// Tries to combine <paramref name="box"/> and <paramref name="toUnion"/> into a single box without changing the total area
    /// </summary>
    public static bool TryUnionInEqualSizedBox(Box2 box, Box2 toUnion, [NotNullWhen(true)] out Box2? union)
    {
        return TryUnionInEqualSizedBox(box, [toUnion], out union);
    }

    /// <summary>
    /// Tries to combine <paramref name="box"/> and <paramref name="toUnion"/> into a single box without changing the total area
    /// </summary>
    public static bool TryUnionInEqualSizedBox(Box2 box, IEnumerable<Box2> toUnion, [NotNullWhen(true)] out Box2? union)
    {
        var result = new Box2(box.BottomLeft, box.TopRight);
        var totalArea = Box2.Area(box);
        foreach (var box2 in toUnion)
        {
            var inter = box.Intersect(box2);
            totalArea += Box2.Area(box2) - Box2.Area(inter);

            result = result.Union(box2);
        }

        union = totalArea == Box2.Area(result) ? result : null;
        return union != null;
    }

    /// <summary>
    /// Returns a new array of boxes in which, if possibe, the <paramref name="boxes"/> are combined without changing the total area
    /// </summary>
    public static List<Box2> UnionInEqualSizedBoxes(IEnumerable<Box2> boxes)
    {
        var result = boxes.ToList();
        var united = true;

        while (united)
        {
            var newArray = new List<Box2>();
            united = false;
            var used = new bool[result.Count];

            for (var i = 0; i < result.Count; i++)
            {
                if (used[i])
                    continue;

                var current = result[i];
                var intersects = GetIntersectedBoxes(i);
                for (var k = 1; k <= intersects.Count; k++)
                {
                    if (used[i])
                        break;

                    var keys = intersects.Keys.Where(e => !used.ElementAt(e)).ToArray();
                    var combinations = GetCombinations(keys, k, 0);
                    foreach (var combination in combinations)
                    {
                        var boxesToUnion = combination.Select(e => result.ElementAt(e));
                        if (TryUnionInEqualSizedBox(current, boxesToUnion, out var union))
                        {
                            used[i] = true;
                            united = true;
                            newArray.Add(union.Value);
                            foreach (var index in combination)
                                used[index] = true;

                            break;
                        }
                    }
                }

                if (!used[i])
                    newArray.Add(current);

                used[i] = true;
            }

            result = newArray;
        }

        return result;

        Dictionary<int, Box2> GetIntersectedBoxes(int index)
        {
            var box = result[index];
            var dict = new Dictionary<int, Box2>();
            for (var i = 0; i < result.Count; i++)
            {
                if (i == index)
                    continue;

                var current = result[i];
                if (!box.Intersect(current).IsEmpty())
                    dict.Add(i, current);
            }

            return dict;
        }

        List<List<T>> GetCombinations<T>(T[] array, int k, int startIndex)
        {
            var result = new List<List<T>>();

            if (k == 0)
            {
                result.Add([]);
                return result;
            }

            for (var i = startIndex; i <= array.Length - k; i++)
            {
                var tailCombinations = GetCombinations(array, k - 1, i + 1);
                foreach (var tail in tailCombinations)
                {
                    var combination = new List<T> { array[i] };
                    combination.AddRange(tail);
                    result.Add(combination);
                }
            }

            return result;
        }
    }

    /// <inheritdoc cref="AttachToGrid(Box2, Vector2)"/>
    public static Box2 AttachToGrid(Box2 box, float gridSize = 1f)
    {
        return AttachToGrid(box, new Vector2(gridSize, gridSize));
    }

    /// <summary>
    /// Creates a new <see cref="Box2"/> based on the <paramref name="box"/> and the specified <paramref name="gridSize"/>
    /// It aligns the original box to fit within the grid.
    /// </summary>
    public static Box2 AttachToGrid(Box2 box, Vector2 gridSize)
    {
        var left = (float)Math.Floor(box.Left / gridSize.X) * gridSize.X;
        var bottom = (float)Math.Floor(box.Bottom / gridSize.Y) * gridSize.Y;
        var right = (float)Math.Ceiling(box.Right / gridSize.X) * gridSize.X;
        var top = (float)Math.Ceiling(box.Top / gridSize.Y) * gridSize.Y;

        if (right == left)
            right += gridSize.X;

        if (top == bottom)
            top += gridSize.Y;

        return new Box2(left, bottom, right, top);
    }

    /// <inheritdoc cref="AttachToGrid(IEnumerable{Box2}, Vector2)"/>
    public static List<Box2> AttachToGrid(IEnumerable<Box2> boxes, float gridSize = 1f)
    {
        return AttachToGrid(boxes, new Vector2(gridSize, gridSize));
    }

    /// <summary>
    /// Creates a new array of <see cref="Box2"/> based on the <paramref name="box"/> and the specified <paramref name="gridSize"/>.
    /// It aligns the original box to fit within the grid.
    /// </summary>
    public static List<Box2> AttachToGrid(IEnumerable<Box2> boxes, Vector2 gridSize)
    {
        var result = new List<Box2>();
        foreach (var box in boxes)
            result.Add(AttachToGrid(box, gridSize));

        return result;
    }
}
