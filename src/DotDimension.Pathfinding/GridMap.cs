using System;

namespace DotDimension.Pathfinding;

public sealed class GridMap
{
    private readonly ulong[] _bits;

    /// <summary>Read-only access to the raw words, for serialization.</summary>
    public ReadOnlySpan<ulong> Bits => _bits;

    public int Width { get; }
    public int Height { get; }

    /// <summary>Total number of cells (Width × Height).</summary>
    public int CellCount { get; }

    public GridMap(int width, int height, bool defaultWalkable = false)
    {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width), width, "Width must be positive.");
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height), height, "Height must be positive.");

        Width = width;
        Height = height;
        CellCount = width * height;

        //* Round the cell count up to a multiple of 64 to get the word count.
        _bits = new ulong[(CellCount + 63) >> 6];

        if (defaultWalkable)
        {
            _bits.AsSpan().Fill(ulong.MaxValue);
            //* clear the unused high bits
            var tail = CellCount & 63;
            if (tail != 0)
            {
                _bits[^1] = ulong.MaxValue >> (64 - tail);
            }
        }
    }

    public bool InBounds(int x, int y)
    {
        return (uint)x < (uint)Width && (uint)y < (uint)Height;
    }

    public int ToIndex(int x, int y)
    {
        if (!InBounds(x, y))
        {
            throw new ArgumentOutOfRangeException($"({x}, {y}) is outside the map.");
        }

        return y * Width + x;
    }

    public int ToIndex(GridPoint point)
    {
        return ToIndex(point.X, point.Y);
    }

    public GridPoint ToPoint(int cellIndex)
    {
        if ((uint)cellIndex >= (uint)CellCount)
        {
            throw new ArgumentOutOfRangeException(nameof(cellIndex));
        }

        return new GridPoint(cellIndex % Width, cellIndex / Width);
    }

    //* public api of the IsWalkableUnchecked by cellIndex
    public bool IsWalkable(int cellIndex)
    {
        if ((uint)cellIndex >= (uint)CellCount)
        {
            throw new ArgumentOutOfRangeException(nameof(cellIndex));
        }

        return IsWalkableUnchecked(cellIndex);
    }

    //* public api of the IsWalkableUnchecked by x,y coordinates
    public bool IsWalkable(int x, int y)
    {
        if (!InBounds(x, y))
        {
            throw new ArgumentOutOfRangeException($"({x}, {y}) is outside the map.");
        }

        return IsWalkableUnchecked(y * Width + x);
    }

    internal bool IsWalkableUnchecked(int cellIndex)
    {
        var word = _bits[cellIndex >> 6]; //* find the word containing the cell. if cellIndex = 37, word = 0
        var bitIndex = cellIndex & 63; //* where is the cell in the word? if cellIndex = 37, bitIndex = 37
        var mask = 1UL << bitIndex;
        if ((word & mask) == 0) return false;
        return true;
        //* return (_bits[cellIndex >> 6] & (1UL << (cellIndex & 63))) != 0;
    }

    public void SetWalkable(int cellIndex, bool value)
    {
        //* if (cellIndex < 0 || cellIndex >= CellCount)
        if ((uint)cellIndex >= (uint)CellCount)
        {
            throw new ArgumentOutOfRangeException(nameof(cellIndex));
        }

        var mask = 1UL << (cellIndex & 63);
        if (value)
        {
            //* for walkable, use OR. set the target bit
            //* 1 | 1 = 1
            //* 1 | 0 = 1
            _bits[cellIndex >> 6] |= mask;
        }
        else
        {
            //* for not walkable, use AND with the inverted mask. clears the target bit.
            //* 1 & 1 = 1
            _bits[cellIndex >> 6] &= ~mask;
        }
    }

    public void SetWalkable(int x, int y, bool value)
    {
        if (!InBounds(x, y))
        {
            throw new ArgumentOutOfRangeException($"({x}, {y}) is outside the map.");
        }

        SetWalkable(y * Width + x, value);
    }

    public static GridMap FromBits(int width, int height, ReadOnlySpan<ulong> bits)
    {
        var map = new GridMap(width, height);
        if (bits.Length != map._bits.Length)
        {
            throw new ArgumentException(
                $"Expected {map._bits.Length} words, got {bits.Length}.", nameof(bits));
        }

        bits.CopyTo(map._bits);
        return map;
    }

    public static GridMap FromBools(bool[,] walkable)
    {
        if (walkable is null)
        {
            throw new ArgumentNullException(nameof(walkable));
        }

        var map = new GridMap(walkable.GetLength(0), walkable.GetLength(1));
        for (var y = 0; y < map.Height; y++)
        {
            for (var x = 0; x < map.Width; x++)
            {
                if (walkable[x, y])
                {
                    map.SetWalkable(map.ToIndex(x, y), true);
                }
            }
        }

        return map;
    }
}
