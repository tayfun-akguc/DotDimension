using System;
using DotDimension.Pathfinding.Internal;

namespace DotDimension.Pathfinding;

public static class GridPathfinder
{
    public const int NoPath = -1;

    public static int FindPath(
        GridMap map, GridSearchBuffers buffers,
        int startCell, int goalCell, Span<int> outPath)
    {
        ValidateArguments(map, buffers, startCell);
        if ((uint)goalCell >= (uint)map.CellCount)
        {
            throw new ArgumentOutOfRangeException(nameof(goalCell));
        }

        if (!map.IsWalkableUnchecked(startCell) || !map.IsWalkableUnchecked(goalCell))
        {
            return NoPath;
        }

        if (startCell == goalCell)
        {
            if (outPath.IsEmpty)
            {
                throw new ArgumentException("outPath is too small.", nameof(outPath));
            }

            outPath[0] = startCell;
            return 1;
        }

        var width = map.Width;
        var goalX = goalCell % width;
        var goalY = goalCell / width;

        buffers.SearchId++;
        var searchId = buffers.SearchId;
        buffers.HeapCount = 0;

        buffers.VisitStamp[startCell] = searchId;
        buffers.G[startCell] = 0f;
        buffers.CameFrom[startCell] = startCell;

        Push(buffers, startCell, Manhattan(startCell % width, startCell / width, goalX, goalY));

        //* main loop
        while (buffers.HeapCount > 0)
        {
            var cell = MinHeap.Pop(
                buffers.HeapItems, buffers.HeapKeys, ref buffers.HeapCount, out var poppedF);

            //* lazy delete
            var x = cell % width;
            var y = cell / width;
            if (poppedF > buffers.G[cell] + Manhattan(x, y, goalX, goalY))
            {
                continue;
            }

            if (cell == goalCell)
            {
                return Reconstruct(buffers, startCell, goalCell, outPath);
            }

            var g = buffers.G[cell];

            if (x > 0)
            {
                Relax(map, buffers, searchId, cell, cell - 1, g, goalX, goalY);
            }

            if (x < width - 1)
            {
                Relax(map, buffers, searchId, cell, cell + 1, g, goalX, goalY);
            }

            if (y > 0)
            {
                Relax(map, buffers, searchId, cell, cell - width, g, goalX, goalY);
            }

            if (cell + width < map.CellCount)
            {
                Relax(map, buffers, searchId, cell, cell + width, g, goalX, goalY);
            }
        }

        return NoPath;
    }

    public static int FindPath(
        GridMap map, GridSearchBuffers buffers,
        GridPoint start, GridPoint goal, Span<int> outPath)
    {
        return FindPath(map, buffers, map.ToIndex(start), map.ToIndex(goal), outPath);
    }

    public static int FindCosts(
        GridMap map, GridSearchBuffers buffers,
        int startCell, ReadOnlySpan<int> targetCells, Span<float> outCosts)
    {
        ValidateArguments(map, buffers, startCell);
        if (outCosts.Length != targetCells.Length)
        {
            throw new ArgumentException(
                "outCosts must have the same length as targetCells.", nameof(outCosts));
        }

        for (var t = 0; t < targetCells.Length; t++)
        {
            if ((uint)targetCells[t] >= (uint)map.CellCount)
            {
                throw new ArgumentOutOfRangeException(nameof(targetCells));
            }

            outCosts[t] = float.PositiveInfinity;
        }

        if (!map.IsWalkableUnchecked(startCell) || targetCells.IsEmpty)
        {
            return 0;
        }

        var width = map.Width;
        var remaining = targetCells.Length;
        var reached = 0;

        buffers.SearchId++;
        var searchId = buffers.SearchId;
        buffers.HeapCount = 0;

        buffers.VisitStamp[startCell] = searchId;
        buffers.G[startCell] = 0f;
        buffers.CameFrom[startCell] = startCell;
        Push(buffers, startCell, 0f);

        while (buffers.HeapCount > 0)
        {
            var cell = MinHeap.Pop(
                buffers.HeapItems, buffers.HeapKeys, ref buffers.HeapCount, out var poppedG);

            if (poppedG > buffers.G[cell])
            {
                continue;
            }

            for (var t = 0; t < targetCells.Length; t++)
            {
                if (targetCells[t] == cell && float.IsPositiveInfinity(outCosts[t]))
                {
                    outCosts[t] = poppedG;
                    reached++;
                    remaining--;
                }
            }

            if (remaining == 0)
            {
                break;
            }

            var g = buffers.G[cell];
            var x = cell % width;

            if (x > 0)
            {
                RelaxNoHeuristic(map, buffers, searchId, cell, cell - 1, g);
            }

            if (x < width - 1)
            {
                RelaxNoHeuristic(map, buffers, searchId, cell, cell + 1, g);
            }

            if (cell - width >= 0)
            {
                RelaxNoHeuristic(map, buffers, searchId, cell, cell - width, g);
            }

            if (cell + width < map.CellCount)
            {
                RelaxNoHeuristic(map, buffers, searchId, cell, cell + width, g);
            }
        }

        return reached;
    }

    private static void ValidateArguments(GridMap map, GridSearchBuffers buffers, int startCell)
    {
        if (map is null)
        {
            throw new ArgumentNullException(nameof(map));
        }

        if (buffers is null)
        {
            throw new ArgumentNullException(nameof(buffers));
        }

        if (!buffers.Fits(map))
        {
            throw new ArgumentException(
                $"Buffers capacity {buffers.Capacity} is smaller than the map's {map.CellCount} cells.",
                nameof(buffers));
        }

        if ((uint)startCell >= (uint)map.CellCount)
        {
            throw new ArgumentOutOfRangeException(nameof(startCell));
        }
    }

    private static float Manhattan(int x1, int y1, int x2, int y2)
    {
        return Math.Abs(x1 - x2) + Math.Abs(y1 - y2);
    }

    private static void Relax(
        GridMap map, GridSearchBuffers buffers, int searchId,
        int fromCell, int toCell, float fromG, int goalX, int goalY)
    {
        if (!map.IsWalkableUnchecked(toCell))
        {
            return;
        }

        var newG = fromG + 1f;
        var seen = buffers.VisitStamp[toCell] == searchId;
        if (seen && newG >= buffers.G[toCell])
        {
            return;
        }

        buffers.VisitStamp[toCell] = searchId;
        buffers.G[toCell] = newG;
        buffers.CameFrom[toCell] = fromCell;

        var width = map.Width;
        var f = newG + Manhattan(toCell % width, toCell / width, goalX, goalY);
        Push(buffers, toCell, f);
    }

    private static void RelaxNoHeuristic(
        GridMap map, GridSearchBuffers buffers, int searchId,
        int fromCell, int toCell, float fromG)
    {
        if (!map.IsWalkableUnchecked(toCell))
        {
            return;
        }

        var newG = fromG + 1f;
        var seen = buffers.VisitStamp[toCell] == searchId;
        if (seen && newG >= buffers.G[toCell])
        {
            return;
        }

        buffers.VisitStamp[toCell] = searchId;
        buffers.G[toCell] = newG;
        buffers.CameFrom[toCell] = fromCell;
        Push(buffers, toCell, newG);
    }

    private static void Push(GridSearchBuffers buffers, int cell, float key)
    {
        if (buffers.HeapCount == buffers.HeapItems.Length)
        {
            throw new InvalidOperationException(
                "Open-set overflow — heap capacity exceeded. Please report this map as an issue.");
        }

        MinHeap.Push(buffers.HeapItems, buffers.HeapKeys, ref buffers.HeapCount, cell, key);
    }

    private static int Reconstruct(
        GridSearchBuffers buffers, int startCell, int goalCell, Span<int> outPath)
    {
        var length = 1;
        for (var cell = goalCell; cell != startCell; cell = buffers.CameFrom[cell])
        {
            length++;
        }

        if (length > outPath.Length)
        {
            throw new ArgumentException(
                $"outPath is too small: path has {length} cells, span holds {outPath.Length}.",
                nameof(outPath));
        }

        var index = length;
        for (var cell = goalCell; cell != startCell; cell = buffers.CameFrom[cell])
        {
            outPath[--index] = cell;
        }

        outPath[0] = startCell;
        return length;
    }
}
