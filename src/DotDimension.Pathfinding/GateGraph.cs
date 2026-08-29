using System;
using System.Collections.Generic;

namespace DotDimension.Pathfinding;

public sealed class GateGraph
{
    public GateLayout Layout { get; }

    public int NodeCount { get; }

    public int EdgeCount { get; }

    //* csr
    internal readonly int[] EdgeStart; //* length NodeCount + 1
    internal readonly int[] EdgeTarget; //* length EdgeCount
    internal readonly float[] EdgeCost; //* length EdgeCount

    internal GateGraph(GateLayout layout, int[] edgeStart, int[] edgeTarget, float[] edgeCost)
    {
        Layout = layout;
        NodeCount = layout.GateCount;
        EdgeCount = edgeTarget.Length;
        EdgeStart = edgeStart;
        EdgeTarget = edgeTarget;
        EdgeCost = edgeCost;
    }

    public int DegreeOf(int gateId)
    {
        if ((uint)gateId >= (uint)NodeCount)
        {
            throw new ArgumentOutOfRangeException(nameof(gateId));
        }

        return EdgeStart[gateId + 1] - EdgeStart[gateId];
    }
}

public sealed class GateGraphBuilder
{
    private readonly GateLayout _layout;

    //* the _edge list is temporary and will be cleared after the graph is built
    private readonly List<(int From, int To, float Cost)> _edges = new();

    private bool _built;

    public GateGraphBuilder(GateLayout layout)
    {
        _layout = layout ?? throw new ArgumentNullException(nameof(layout));
    }

    public void AddIntraIslandEdge(int islandIndex, int localGateA, int localGateB, float cost)
    {
        var a = _layout.GateId(islandIndex, localGateA);
        var b = _layout.GateId(islandIndex, localGateB);
        AddEdge(a, b, cost);
    }

    public void AddInterIslandEdge(int islandA, int localGateA, int islandB, int localGateB)
    {
        var a = _layout.GateId(islandA, localGateA);
        var b = _layout.GateId(islandB, localGateB);
        AddEdge(a, b, 1f);
    }

    public void AddEdge(int gateA, int gateB, float cost)
    {
        ThrowIfBuilt();

        _ = _layout.IslandOf(gateA);
        _ = _layout.IslandOf(gateB);

        if (gateA == gateB)
        {
            throw new ArgumentException($"Self-loop on gate {gateA} is not allowed.");
        }

        if (!(cost > 0f) || float.IsInfinity(cost))
        {
            throw new ArgumentOutOfRangeException(
                nameof(cost), cost, "Edge cost must be a positive finite number.");
        }

        //* add two undirected
        _edges.Add((gateA, gateB, cost));
        _edges.Add((gateB, gateA, cost));
    }

    public GateGraph Build()
    {
        ThrowIfBuilt();
        _built = true;

        var nodeCount = _layout.GateCount;
        var edgeStart = new int[nodeCount + 1];

        foreach (var (from, _, _) in _edges)
        {
            edgeStart[from + 1]++;
        }

        for (var n = 0; n < nodeCount; n++)
        {
            edgeStart[n + 1] += edgeStart[n];
        }

        var cursor = new int[nodeCount];
        var edgeTarget = new int[_edges.Count];
        var edgeCost = new float[_edges.Count];

        foreach (var (from, to, cost) in _edges)
        {
            var slot = edgeStart[from] + cursor[from]++;
            edgeTarget[slot] = to;
            edgeCost[slot] = cost;
        }

        return new GateGraph(_layout, edgeStart, edgeTarget, edgeCost);
    }

    private void ThrowIfBuilt()
    {
        if (_built)
        {
            throw new InvalidOperationException(
                "This builder has already produced a graph; create a new builder.");
        }
    }
}
