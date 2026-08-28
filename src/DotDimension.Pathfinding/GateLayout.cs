using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace DotDimension.Pathfinding;

//TODO:
public sealed class GateLayout
{
    //* prefix array, similar to edgeStart in the compressed sparse row
    //* let's say there is for islands = A(3), B(5), C(4), D(2)
    //* so _gateStart = [0, 3, 8, 12, 14]
    //* _gateStart[^1] = 14 = total number of gates = 3 + 5 + 4 + 2
    //* A -> [0, 3)   = ids 0, 1, 2
    //* B -> [3, 8)   = ids 3..7
    //* C -> [8, 12)  = ids 8..11
    //* D -> [12, 14) = ids 12, 13
    //* gate count of islandB => _gateStart[2] − _gateStart[1] = 8 - 3 = 5 gates
    //* global id of isnlandB's local gate 4 => _gateStart[1] + 4 = 3 + 4 = 7
    //* which island owns the gate 9 => [8, 12) = islandC (binary search)
    private readonly int[] _gateStart;

    //* gate world positions, indexed by global gate id
    //* gateStart   = [0, 3, 8, 12, 14]
    //* gate id     = [  0,   1,   2,   3,   4, ...,  13]
    //* posx        = [210, 210, 210, 250, 250, ..., 670]
    //* posy        = [190, 260, 330, 190, 260, ..., 340]
    //* gates of islandA = (210, 190), (210, 260), (210, 330), (250, 190)
    //* what is the position of gate0 => (posX[0], posY[0]) = (210, 190)
    private readonly int[] _posX;
    private readonly int[] _posY;

    //* number of islands in the world
    public int IslandCount { get; }

    public int GateCount { get; }

    private GateLayout(int[] gateStart, int[] posX, int[] posY)
    {
        _gateStart = gateStart;
        _posX = posX;
        _posY = posY;
        IslandCount = gateStart.Length - 1;
        GateCount = posX.Length;
    }

    public static GateLayout Create(IReadOnlyList<IReadOnlyList<GridPoint>> islands)
    {
        if (islands is null)
        {
            throw new ArgumentNullException(nameof(islands));
        }

        var gateStart = new int[islands.Count + 1];
        for (var i = 0; i < islands.Count; i++)
        {
            var island = islands[i]
                         ?? throw new ArgumentException($"Island {i} is null.", nameof(islands));
            gateStart[i + 1] = gateStart[i] + island.Count;
        }

        var gateCount = gateStart[^1];
        var posX = new int[gateCount];
        var posY = new int[gateCount];

        for (var i = 0; i < islands.Count; i++)
        {
            var island = islands[i];
            var baseId = gateStart[i];
            for (var g = 0; g < island.Count; g++)
            {
                posX[baseId + g] = island[g].X;
                posY[baseId + g] = island[g].Y;
            }
        }

        return new GateLayout(gateStart, posX, posY);
    }

    public int GateCountOf(int islandIndex)
    {
        if ((uint)islandIndex >= (uint)IslandCount)
        {
            throw new ArgumentOutOfRangeException(nameof(islandIndex));
        }

        return _gateStart[islandIndex + 1] - _gateStart[islandIndex];
    }

    public int FirstGateOf(int islandIndex)
    {
        if ((uint)islandIndex >= (uint)IslandCount)
        {
            throw new ArgumentOutOfRangeException(nameof(islandIndex));
        }

        return _gateStart[islandIndex];
    }

    public int GateId(int islandIndex, int localGate)
    {
        if ((uint)islandIndex >= (uint)IslandCount)
        {
            throw new ArgumentOutOfRangeException(nameof(islandIndex));
        }

        var start = _gateStart[islandIndex];
        var count = _gateStart[islandIndex + 1] - start;
        if ((uint)localGate >= (uint)count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(localGate),
                $"Island {islandIndex} has {count} gates; local gate {localGate} does not exist.");
        }

        return start + localGate;
    }

    public int IslandOf(int gateId)
    {
        if ((uint)gateId >= (uint)GateCount)
        {
            throw new ArgumentOutOfRangeException(nameof(gateId));
        }

        var index = Array.BinarySearch(_gateStart, gateId);
        return index >= 0
            ? SkipEmptyIslands(index)
            : ~index - 1;
    }

    private int SkipEmptyIslands(int index)
    {
        while (index + 1 < _gateStart.Length && _gateStart[index + 1] == _gateStart[index])
        {
            index++;
        }

        return index;
    }

    public int LocalGateOf(int gateId)
    {
        return gateId - _gateStart[IslandOf(gateId)];
    }

    public GridPoint PositionOf(int gateId)
    {
        if ((uint)gateId >= (uint)GateCount)
        {
            throw new ArgumentOutOfRangeException(nameof(gateId));
        }

        return new GridPoint(_posX[gateId], _posY[gateId]);
    }

    internal void PositionUnchecked(int gateId, out int x, out int y)
    {
        x = _posX[gateId];
        y = _posY[gateId];
    }
}
