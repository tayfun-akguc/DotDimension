using System;

namespace DotDimension.Pathfinding;

public static class GatePrecompute
{
    public static int AddIntraIslandEdges(
        GateGraphBuilder builder, int islandIndex,
        GridMap islandMap, GridSearchBuffers buffers,
        ReadOnlySpan<int> gateCells)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        var gateCount = gateCells.Length;
        if (gateCount < 2)
        {
            return 0;
        }

        Span<float> costs = gateCount <= 64 ? stackalloc float[gateCount] : new float[gateCount];

        var added = 0;
        for (var a = 0; a < gateCount - 1; a++)
        {
            var targets = gateCells[(a + 1)..];
            var sweep = costs[..targets.Length];
            GridPathfinder.FindCosts(islandMap, buffers, gateCells[a], targets, sweep);

            for (var offset = 0; offset < targets.Length; offset++)
            {
                var cost = sweep[offset];
                if (float.IsPositiveInfinity(cost))
                {
                    continue;
                }

                builder.AddIntraIslandEdge(islandIndex, a, a + 1 + offset, cost);
                added++;
            }
        }

        return added;
    }

    public static int AddIntraIslandEdges(
        GateGraphBuilder builder, GateLayout layout, int islandIndex,
        GridMap islandMap, GridSearchBuffers buffers)
    {
        if (layout is null)
        {
            throw new ArgumentNullException(nameof(layout));
        }

        if (islandMap is null)
        {
            throw new ArgumentNullException(nameof(islandMap));
        }

        var gateCount = layout.GateCountOf(islandIndex);
        Span<int> gateCells = gateCount <= 64 ? stackalloc int[gateCount] : new int[gateCount];

        var firstGate = layout.FirstGateOf(islandIndex);
        for (var g = 0; g < gateCount; g++)
        {
            var position = layout.PositionOf(firstGate + g);
            gateCells[g] = islandMap.ToIndex(position);
        }

        return AddIntraIslandEdges(builder, islandIndex, islandMap, buffers, gateCells);
    }
}
