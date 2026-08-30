namespace DotDimension.Pathfinding.Tests;

public sealed class EndToEndRouteTests
{

    //* Island A castle (1,4)
    //* (7,0)....(7,7) => first row
    //* ..............
    //* ..............
    //* (0,0)....(7,0) => last row
    //* (0,0) bottom left corner.
    private const string IslandA = """
                                   ........
                                   ........
                                   ........
                                   .....##+
                                   ......#.
                                   ......#.
                                   ........
                                   ........
                                   """;


    //* Island B (6,2) (world (14,2)
    private const string IslandB = """
                                   ........
                                   ........
                                   ........
                                   +#......
                                   .#......
                                   ........
                                   ........
                                   ........
                                   """;

    private static readonly GridPoint CastleA = new(1, 4);
    private static readonly GridPoint CastleBLocal = new(6, 2);
    private static readonly GridPoint OriginB = new(8, 0);

    private static (GridMap Map, GridPoint[] Gates) ParseIsland(string drawing)
    {
        var rows = drawing.Split('\n',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var height = rows.Length;
        var width = rows[0].Length;

        var map = new GridMap(width, height);
        var gates = new List<GridPoint>();

        for (var r = 0; r < height; r++)
        {
            Assert.Equal(width, rows[r].Length);
            var y = height - 1 - r;
            for (var x = 0; x < width; x++)
            {
                var symbol = rows[r][x];
                if (symbol == '.' || symbol == '+')
                {
                    map.SetWalkable(x, y, true);
                }

                if (symbol == '+')
                {
                    gates.Add(new GridPoint(x, y));
                }
            }
        }

        return (map, gates.ToArray());
    }

    private sealed record World(
        GridMap MapA,
        GridMap MapB,
        GateGraph Graph,
        GridSearchBuffers GridBuffers,
        GateSearchBuffers GateBuffers,
        float[] CastleToGates,
        float[] GatesToCastle);

    private static World BuildWorld()
    {
        var (mapA, gatesA) = ParseIsland(IslandA);
        var (mapB, gatesB) = ParseIsland(IslandB);
        Assert.Single(gatesA);
        Assert.Single(gatesB);

        var layout = GateLayout.Create(new[]
        {
            gatesA,
            gatesB.Select(g => new GridPoint(g.X + OriginB.X, g.Y + OriginB.Y)).ToArray(),
        });

        var gridBuffers = new GridSearchBuffers(Math.Max(mapA.CellCount, mapB.CellCount));
        var builder = new GateGraphBuilder(layout);

        GatePrecompute.AddIntraIslandEdges(
            builder, 0, mapA, gridBuffers, gatesA.Select(mapA.ToIndex).ToArray());
        GatePrecompute.AddIntraIslandEdges(
            builder, 1, mapB, gridBuffers, gatesB.Select(mapB.ToIndex).ToArray());
        builder.AddInterIslandEdge(0, 0, 1, 0);

        var graph = builder.Build();

        var castleToGates = new float[1];
        GridPathfinder.FindCosts(
            mapA, gridBuffers, mapA.ToIndex(CastleA),
            new[] { mapA.ToIndex(gatesA[0]) }, castleToGates);

        var gatesToCastle = new float[1];
        GridPathfinder.FindCosts(
            mapB, gridBuffers, mapB.ToIndex(CastleBLocal),
            new[] { mapB.ToIndex(gatesB[0]) }, gatesToCastle);

        return new World(mapA, mapB, graph, gridBuffers,
            new GateSearchBuffers(graph), castleToGates, gatesToCastle);
    }

    private static bool IsWorldWalkable(World world, GridPoint p)
    {
        return p.X < 8
            ? world.MapA.IsWalkable(p.X, p.Y)
            : world.MapB.IsWalkable(p.X - OriginB.X, p.Y - OriginB.Y);
    }

    [Fact]
    public void Full_pipeline_produces_the_optimal_cell_route()
    {
        var world = BuildWorld();

        Assert.Equal(8f, world.CastleToGates[0]);
        Assert.Equal(8f, world.GatesToCastle[0]);

        Span<int> chain = stackalloc int[8];
        var chainLength = GateChainSearch.Find(
            world.Graph, world.GateBuffers,
            originIsland: 0, world.CastleToGates, CastleA,
            destinationIsland: 1, world.GatesToCastle, new GridPoint(14, 2),
            directCost: float.PositiveInfinity, chain);

        Assert.Equal(2, chainLength);
        Assert.Equal(new[] { 0, 1 }, chain[..2].ToArray());

        var layout = world.Graph.Layout;
        var gateALocal = layout.PositionOf(0);
        var gateBWorld = layout.PositionOf(1);
        var gateBLocal = new GridPoint(gateBWorld.X - OriginB.X, gateBWorld.Y - OriginB.Y);

        Span<int> cells = stackalloc int[64];
        var lengthA = GridPathfinder.FindPath(
            world.MapA, world.GridBuffers,
            world.MapA.ToIndex(CastleA), world.MapA.ToIndex(gateALocal), cells);
        var segmentA = ToPoints(world.MapA, cells[..lengthA]);

        var lengthB = GridPathfinder.FindPath(
            world.MapB, world.GridBuffers,
            world.MapB.ToIndex(gateBLocal), world.MapB.ToIndex(CastleBLocal), cells);
        var segmentB = ToPoints(world.MapB, cells[..lengthB]);

        Span<GridPoint> routeBuffer = stackalloc GridPoint[64];
        var stitcher = new PathStitcher(routeBuffer);
        stitcher.Append(segmentA);
        stitcher.Append(segmentB, OriginB);
        var route = stitcher.Path;

        Assert.Equal(18, route.Length);
        Assert.Equal(CastleA, route[0]);
        Assert.Equal(new GridPoint(14, 2), route[^1]);

        var expectedSteps = world.CastleToGates[0] + 1f + world.GatesToCastle[0];
        Assert.Equal(expectedSteps, route.Length - 1f);

        for (var i = 1; i < route.Length; i++)
        {
            var manhattan = Math.Abs(route[i].X - route[i - 1].X)
                            + Math.Abs(route[i].Y - route[i - 1].Y);
            Assert.Equal(1, manhattan);
        }

        foreach (var point in route)
        {
            Assert.True(IsWorldWalkable(world, point), $"({point.X}, {point.Y}) is not walkable.");
        }

        var crossingIndex = route.ToArray().ToList().FindIndex(p => p.X == 7 && p.Y == 4);
        Assert.True(crossingIndex >= 0);
        Assert.Equal(new GridPoint(8, 4), route[crossingIndex + 1]);
    }

    [Fact]
    public void Full_pipeline_with_string_pulling_keeps_the_contracts()
    {
        var world = BuildWorld();

        Span<int> cells = stackalloc int[64];
        Span<int> waypoints = stackalloc int[64];

        var lengthA = GridPathfinder.FindPath(
            world.MapA, world.GridBuffers,
            world.MapA.ToIndex(CastleA), world.MapA.ToIndex(new GridPoint(7, 4)), cells);
        var waypointCountA = StringPulling.Simplify(world.MapA, cells[..lengthA], waypoints);
        var simplifiedA = ToPoints(world.MapA, waypoints[..waypointCountA]);

        var lengthB = GridPathfinder.FindPath(
            world.MapB, world.GridBuffers,
            world.MapB.ToIndex(new GridPoint(0, 4)), world.MapB.ToIndex(CastleBLocal), cells);
        var waypointCountB = StringPulling.Simplify(world.MapB, cells[..lengthB], waypoints);
        var simplifiedB = ToPoints(world.MapB, waypoints[..waypointCountB]);

        Span<GridPoint> routeBuffer = stackalloc GridPoint[32];
        var stitcher = new PathStitcher(routeBuffer);
        stitcher.Append(simplifiedA);
        stitcher.Append(simplifiedB, OriginB);
        var route = stitcher.Path;

        Assert.True(route.Length < 18);
        Assert.Equal(CastleA, route[0]);
        Assert.Equal(new GridPoint(14, 2), route[^1]);
        Assert.True(waypointCountA < lengthA);
        Assert.True(waypointCountB < lengthB);

        var routeArray = route.ToArray();
        Assert.Contains(new GridPoint(7, 4), routeArray);
        Assert.Contains(new GridPoint(8, 4), routeArray);
    }

    private static GridPoint[] ToPoints(GridMap map, ReadOnlySpan<int> cells)
    {
        var points = new GridPoint[cells.Length];
        for (var i = 0; i < cells.Length; i++)
        {
            points[i] = map.ToPoint(cells[i]);
        }

        return points;
    }
}
