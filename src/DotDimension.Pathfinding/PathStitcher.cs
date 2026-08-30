using System;

namespace DotDimension.Pathfinding;

public ref struct PathStitcher
{
    private readonly Span<GridPoint> _destination;
    private int _count;

    public PathStitcher(Span<GridPoint> destination)
    {
        _destination = destination;
        _count = 0;
    }

    public int Count => _count;

    public ReadOnlySpan<GridPoint> Path => _destination[.._count];

    public void Append(ReadOnlySpan<GridPoint> segment)
    {
        Append(segment, new GridPoint(0, 0));
    }

    public void Append(ReadOnlySpan<GridPoint> localSegment, GridPoint islandOrigin)
    {
        if (localSegment.IsEmpty)
        {
            return;
        }

        var copyFrom = 0;
        if (_count > 0)
        {
            var last = _destination[_count - 1];
            var first = Translate(localSegment[0], islandOrigin);
            var manhattan = Math.Abs(first.X - last.X) + Math.Abs(first.Y - last.Y);

            if (manhattan == 0)
            {
                copyFrom = 1;
            }
            else if (manhattan != 1)
            {
                throw new ArgumentException(
                    $"Discontinuous segment: route ends at ({last.X}, {last.Y}) but the " +
                    $"segment starts at ({first.X}, {first.Y}). Segments must join on the " +
                    "same cell or on adjacent cells (a gate crossing).",
                    nameof(localSegment));
            }
        }

        for (var i = copyFrom; i < localSegment.Length; i++)
        {
            if (_count == _destination.Length)
            {
                throw new ArgumentException(
                    "Destination buffer is too small for the stitched route.");
            }

            _destination[_count++] = Translate(localSegment[i], islandOrigin);
        }
    }

    private static GridPoint Translate(GridPoint point, GridPoint origin)
    {
        return new GridPoint(point.X + origin.X, point.Y + origin.Y);
    }
}
