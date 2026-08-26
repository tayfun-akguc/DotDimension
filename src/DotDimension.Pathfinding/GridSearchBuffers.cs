using System;

namespace DotDimension.Pathfinding;

public sealed class GridSearchBuffers
{
    public int Capacity { get; }

    internal readonly float[] G;
    internal readonly int[] CameFrom;
    internal readonly int[] VisitStamp;

    internal readonly int[] HeapItems;
    internal readonly float[] HeapKeys;
    internal int HeapCount;

    internal int SearchId;

    public GridSearchBuffers(GridMap map)
        : this(map?.CellCount ?? throw new ArgumentNullException(nameof(map)))
    {
    }

    public GridSearchBuffers(int cellCapacity)
    {
        if (cellCapacity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(cellCapacity), cellCapacity, "Capacity must be positive.");
        }

        Capacity = cellCapacity;
        G = new float[cellCapacity];
        CameFrom = new int[cellCapacity];
        VisitStamp = new int[cellCapacity];
        HeapItems = new int[cellCapacity];
        HeapKeys = new float[cellCapacity];
    }

    public bool Fits(GridMap map)
    {
        if (map is null)
        {
            throw new ArgumentNullException(nameof(map));
        }

        return map.CellCount <= Capacity;
    }
}
