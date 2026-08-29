using System;

namespace DotDimension.Pathfinding;

public sealed class GateSearchBuffers
{
    public int Capacity { get; }

    internal readonly float[] G;
    internal readonly int[] CameFrom;
    internal readonly int[] VisitStamp;

    internal int[] HeapItems;
    internal float[] HeapKeys;
    internal int HeapCount;

    internal int SearchId;

    public GateSearchBuffers(GateGraph graph, int initialHeapCapacity = 1024)
    {
        if (graph is null)
        {
            throw new ArgumentNullException(nameof(graph));
        }

        if (initialHeapCapacity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(initialHeapCapacity), initialHeapCapacity,
                "Initial heap capacity must be positive.");
        }

        //* for origin and destination need 2 more spaces
        Capacity = graph.NodeCount + 2;
        G = new float[Capacity];
        CameFrom = new int[Capacity];
        VisitStamp = new int[Capacity];
        HeapItems = new int[initialHeapCapacity];
        HeapKeys = new float[initialHeapCapacity];
        HeapCount = 0;
        SearchId = 0;
    }

    public bool Fits(GateGraph graph)
    {
        if (graph is null)
        {
            throw new ArgumentNullException(nameof(graph));
        }

        return graph.NodeCount + 2 <= Capacity;
    }
}
