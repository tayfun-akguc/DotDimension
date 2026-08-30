using System;

namespace DotDimension.Pathfinding;

public static class StringPulling
{
    public static bool HasLineOfSight(GridMap map, int fromCell, int toCell)
    {
        if (map is null)
        {
            throw new ArgumentNullException(nameof(map));
        }

        if ((uint)fromCell >= (uint)map.CellCount)
        {
            throw new ArgumentOutOfRangeException(nameof(fromCell));
        }

        if ((uint)toCell >= (uint)map.CellCount)
        {
            throw new ArgumentOutOfRangeException(nameof(toCell));
        }

        var width = map.Width;
        var x = fromCell % width;
        var y = fromCell / width;
        var x1 = toCell % width;
        var y1 = toCell / width;

        if (!map.IsWalkableUnchecked(fromCell))
        {
            return false;
        }

        var nx = Math.Abs(x1 - x);
        var ny = Math.Abs(y1 - y);
        var signX = Math.Sign(x1 - x);
        var signY = Math.Sign(y1 - y);

        var ix = 0;
        var iy = 0;
        while (ix < nx || iy < ny)
        {
            var decision = (1 + 2 * ix) * ny - (1 + 2 * iy) * nx;
            if (decision == 0)
            {
                if (!map.IsWalkableUnchecked(y * width + (x + signX)) ||
                    !map.IsWalkableUnchecked((y + signY) * width + x))
                {
                    return false;
                }

                x += signX;
                y += signY;
                ix++;
                iy++;
            }
            else if (decision < 0)
            {
                x += signX;
                ix++;
            }
            else
            {
                y += signY;
                iy++;
            }

            if (!map.IsWalkableUnchecked(y * width + x))
            {
                return false;
            }
        }

        return true;
    }

    public static bool HasLineOfSight(GridMap map, GridPoint from, GridPoint to)
    {
        return HasLineOfSight(map, map.ToIndex(from), map.ToIndex(to));
    }

    public static int Simplify(GridMap map, ReadOnlySpan<int> path, Span<int> outWaypoints)
    {
        if (map is null)
        {
            throw new ArgumentNullException(nameof(map));
        }

        if (path.IsEmpty)
        {
            return 0;
        }

        if (outWaypoints.IsEmpty)
        {
            throw new ArgumentException("outWaypoints is too small.", nameof(outWaypoints));
        }

        var count = 0;
        outWaypoints[count++] = path[0];
        if (path.Length == 1)
        {
            return count;
        }

        var anchor = 0;
        var i = 1;
        while (i < path.Length - 1)
        {
            if (HasLineOfSight(map, path[anchor], path[i + 1]))
            {
                i++;
                continue;
            }

            if (count == outWaypoints.Length)
            {
                throw new ArgumentException(
                    "outWaypoints is too small for the simplified path.", nameof(outWaypoints));
            }

            outWaypoints[count++] = path[i];
            anchor = i;
            i++;
        }

        if (count == outWaypoints.Length)
        {
            throw new ArgumentException(
                "outWaypoints is too small for the simplified path.", nameof(outWaypoints));
        }

        outWaypoints[count++] = path[^1];
        return count;
    }
}
