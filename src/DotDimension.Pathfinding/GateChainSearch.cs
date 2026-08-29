using System;
using DotDimension.Pathfinding.Internal;

namespace DotDimension.Pathfinding;

public static class GateChainSearch
{
    public const int NoRoute = -1;

    public static int Find(
        GateGraph graph, GateSearchBuffers buffers,
        int originIsland, ReadOnlySpan<float> castleToGateCosts, GridPoint originPos,
        int destinationIsland, ReadOnlySpan<float> gateToCastleCosts, GridPoint destinationPos,
        float directCost,
        Span<int> outChain)
    {
        Validate(graph, buffers, originIsland, castleToGateCosts,
            destinationIsland, gateToCastleCosts);

        var layout = graph.Layout;
        var vOrigin = graph.NodeCount;
        var vDestination = graph.NodeCount + 1;

        var destFirst = layout.FirstGateOf(destinationIsland);
        var destEnd = destFirst + gateToCastleCosts.Length;

        buffers.SearchId++;
        var searchId = buffers.SearchId;
        buffers.HeapCount = 0;

        buffers.VisitStamp[vOrigin] = searchId;
        buffers.G[vOrigin] = 0f;
        buffers.CameFrom[vOrigin] = vOrigin;

        Push(buffers, vOrigin, Euclidean(originPos.X, originPos.Y, destinationPos.X, destinationPos.Y));

        while (buffers.HeapCount > 0)
        {
            var node = MinHeap.Pop(
                buffers.HeapItems, buffers.HeapKeys, ref buffers.HeapCount, out var poppedF);

            if (poppedF > buffers.G[node] + HeuristicOf(layout, node, vOrigin, vDestination,
                    originPos, destinationPos))
            {
                continue;
            }

            if (node == vDestination)
            {
                return Reconstruct(buffers, vOrigin, vDestination, outChain);
            }

            var g = buffers.G[node];

            if (node == vOrigin)
            {
                var originFirst = layout.FirstGateOf(originIsland);
                for (var local = 0; local < castleToGateCosts.Length; local++)
                {
                    var cost = castleToGateCosts[local];
                    if (float.IsPositiveInfinity(cost))
                    {
                        continue;
                    }

                    RelaxToGate(graph, buffers, searchId, node, originFirst + local,
                        g + cost, destinationPos);
                }

                if (!float.IsPositiveInfinity(directCost))
                {
                    RelaxToDestination(buffers, searchId, node, g + directCost, vDestination);
                }

                continue;
            }

            var end = graph.EdgeStart[node + 1];
            for (var e = graph.EdgeStart[node]; e < end; e++)
            {
                RelaxToGate(graph, buffers, searchId, node, graph.EdgeTarget[e],
                    g + graph.EdgeCost[e], destinationPos);
            }

            if (node >= destFirst && node < destEnd)
            {
                var cost = gateToCastleCosts[node - destFirst];
                if (!float.IsPositiveInfinity(cost))
                {
                    RelaxToDestination(buffers, searchId, node, g + cost, vDestination);
                }
            }
        }

        return NoRoute;
    }

    private static void Validate(
        GateGraph graph, GateSearchBuffers buffers,
        int originIsland, ReadOnlySpan<float> castleToGateCosts,
        int destinationIsland, ReadOnlySpan<float> gateToCastleCosts)
    {
        if (graph is null)
        {
            throw new ArgumentNullException(nameof(graph));
        }

        if (buffers is null)
        {
            throw new ArgumentNullException(nameof(buffers));
        }

        if (!buffers.Fits(graph))
        {
            throw new ArgumentException(
                $"Buffers capacity {buffers.Capacity} is smaller than required " +
                $"{graph.NodeCount + 2}.", nameof(buffers));
        }

        var originGates = graph.Layout.GateCountOf(originIsland);
        if (castleToGateCosts.Length != originGates)
        {
            throw new ArgumentException(
                $"castleToGateCosts has {castleToGateCosts.Length} entries but island " +
                $"{originIsland} has {originGates} gates.", nameof(castleToGateCosts));
        }

        var destinationGates = graph.Layout.GateCountOf(destinationIsland);
        if (gateToCastleCosts.Length != destinationGates)
        {
            throw new ArgumentException(
                $"gateToCastleCosts has {gateToCastleCosts.Length} entries but island " +
                $"{destinationIsland} has {destinationGates} gates.", nameof(gateToCastleCosts));
        }
    }

    private static float Euclidean(int x1, int y1, int x2, int y2)
    {
        float dx = x1 - x2;
        float dy = y1 - y2;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    private static float HeuristicOf(
        GateLayout layout, int node, int vOrigin, int vDestination,
        GridPoint originPos, GridPoint destinationPos)
    {
        if (node == vDestination)
        {
            return 0f;
        }

        if (node == vOrigin)
        {
            return Euclidean(originPos.X, originPos.Y, destinationPos.X, destinationPos.Y);
        }

        layout.PositionUnchecked(node, out var x, out var y);
        return Euclidean(x, y, destinationPos.X, destinationPos.Y);
    }

    private static void RelaxToGate(
        GateGraph graph, GateSearchBuffers buffers, int searchId,
        int fromNode, int toGate, float newG, GridPoint destinationPos)
    {
        var seen = buffers.VisitStamp[toGate] == searchId;
        if (seen && newG >= buffers.G[toGate])
        {
            return;
        }

        buffers.VisitStamp[toGate] = searchId;
        buffers.G[toGate] = newG;
        buffers.CameFrom[toGate] = fromNode;

        graph.Layout.PositionUnchecked(toGate, out var x, out var y);
        Push(buffers, toGate, newG + Euclidean(x, y, destinationPos.X, destinationPos.Y));
    }

    private static void RelaxToDestination(
        GateSearchBuffers buffers, int searchId, int fromNode, float newG, int vDestination)
    {
        var seen = buffers.VisitStamp[vDestination] == searchId;
        if (seen && newG >= buffers.G[vDestination])
        {
            return;
        }

        buffers.VisitStamp[vDestination] = searchId;
        buffers.G[vDestination] = newG;
        buffers.CameFrom[vDestination] = fromNode;
        Push(buffers, vDestination, newG);
    }

    private static void Push(GateSearchBuffers buffers, int node, float key)
    {
        if (buffers.HeapCount == buffers.HeapItems.Length)
        {
            Array.Resize(ref buffers.HeapItems, buffers.HeapItems.Length * 2);
            Array.Resize(ref buffers.HeapKeys, buffers.HeapKeys.Length * 2);
        }

        MinHeap.Push(buffers.HeapItems, buffers.HeapKeys, ref buffers.HeapCount, node, key);
    }

    private static int Reconstruct(
        GateSearchBuffers buffers, int vOrigin, int vDestination, Span<int> outChain)
    {
        var length = 0;
        for (var node = buffers.CameFrom[vDestination];
             node != vOrigin;
             node = buffers.CameFrom[node])
        {
            length++;
        }

        if (length > outChain.Length)
        {
            throw new ArgumentException(
                $"outChain is too small: chain has {length} gates, span holds {outChain.Length}.",
                nameof(outChain));
        }

        var index = length;
        for (var node = buffers.CameFrom[vDestination];
             node != vOrigin;
             node = buffers.CameFrom[node])
        {
            outChain[--index] = node;
        }

        return length;
    }
}
