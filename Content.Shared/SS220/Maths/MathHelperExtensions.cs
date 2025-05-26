// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Shared.SS220.Maths;

public static partial class MathHelperExtensions
{
    /// <summary>
    /// Substracts the <paramref name="cutter"/> from the <paramref name="boxes"/> returning the remaining sections
    /// </summary>
    public static IEnumerable<Box2> SubstructBox(IEnumerable<Box2> boxes, Box2 cutter)
    {
        var result = new List<Box2>();
        foreach (var box in boxes)
            result.AddRange(SubstructBox(box, cutter));

        return result;
    }

    /// <summary>
    /// Substracts the <paramref name="cutter"/> from the <paramref name="box"/> returning the remaining sections
    /// </summary>
    public static IEnumerable<Box2> SubstructBox(Box2 box, Box2 cutter)
    {
        var result = new List<Box2>();

        var inter = box.Intersect(cutter);
        if (inter == Box2.Empty)
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
    public static IEnumerable<Box2> GetNonOverlappingBoxes(IEnumerable<Box2> boxes)
    {
        var result = new List<Box2>();

        foreach (var box in boxes)
        {
            var currentParts = new List<Box2> { box };

            foreach (var existing in result)
            {
                var newParts = new List<Box2>();
                foreach (var part in currentParts)
                {
                    if (part.IntersectPercentage(existing) > 0)
                    {
                        var subParts = SubstructBox(part, existing);
                        newParts.AddRange(subParts);
                    }
                    else
                    {
                        newParts.Add(part);
                    }
                }
                currentParts = newParts;
            }

            result.AddRange(currentParts);
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
    /// Tries to combine <paramref name="boxes"/> into a single box without changing the total area
    /// </summary>
    public static bool TryUnionInEqualSizedBox(IEnumerable<Box2> boxes, [NotNullWhen(true)] out Box2? union)
    {
        union = null;
        if (!boxes.Any())
            return false;

        var box = boxes.First();
        boxes = boxes.Skip(1);
        return TryUnionInEqualSizedBox(box, boxes, out union);
    }

    /// <summary>
    /// Returns a new array of boxes in which, if possibe, the <paramref name="boxes"/> are combined without changing the total area
    /// </summary>
    public static IEnumerable<Box2> UnionInEqualSizedBoxes(IEnumerable<Box2> boxes)
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
                    var combinations = GetCombinations(keys, k);
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
    }

    /// <inheritdoc cref="GetCombinations{T}(T[], int, int)"/>
    public static IEnumerable<IEnumerable<T>> GetCombinations<T>(T[] array, int k)
    {
        return GetCombinations(array, k, 0);
    }

    /// <summary>
    /// Returns an array of possible combinations of the elements in <paramref name="array"/>
    /// </summary>
    public static IEnumerable<IEnumerable<T>> GetCombinations<T>(T[] array, int k, int startIndex)
    {
        var result = new List<IEnumerable<T>>();

        if (k == 0)
        {
            result.Add(Enumerable.Empty<T>());
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

    /// <summary>
    /// Returns an array of all boxes in the grid that the other <paramref name="box"/> intersects with
    /// </summary>
    public static IEnumerable<Box2> GetIntersectsGridBoxes(Box2 box, float gridSize = 1f, bool closedRegion = true)
    {
        var startBoxX = (int)Math.Floor(box.Left / gridSize);
        var endBoxX = (int)Math.Floor(box.Right / gridSize);
        var startBoxY = (int)Math.Floor(box.Bottom / gridSize);
        var endBoxY = (int)Math.Floor(box.Top / gridSize);

        var result = new List<Box2>();
        for (var bx = startBoxX; bx <= endBoxX; bx++)
        {
            for (var by = startBoxY; by <= endBoxY; by++)
            {
                var left = bx * gridSize;
                var bottom = by * gridSize;
                var right = left + gridSize;
                var top = bottom + gridSize;

                var gridBox = new Box2(left, bottom, right, top);

                //if (gridBox.IntersectPercentage(box) > 0)
                result.Add(gridBox);
            }
        }

        return result;
    }

    /// <summary>
    /// Checks whether at least one vertex of the <paramref name="inner"/> is inside the <paramref name="box"/>.
    /// </summary>
    public static bool ContainsVertex(Box2 box, Box2 inner, bool closedRegion = true)
    {
        return box.Contains(inner.BottomLeft, closedRegion) ||
              box.Contains(inner.TopLeft, closedRegion) ||
              box.Contains(inner.BottomRight, closedRegion) ||
              box.Contains(inner.TopRight, closedRegion);
    }
}
