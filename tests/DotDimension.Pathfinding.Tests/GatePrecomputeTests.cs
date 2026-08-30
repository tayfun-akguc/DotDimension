namespace DotDimension.Pathfinding.Tests;

public sealed class GatePrecomputeTests
{
    private static GridMap Parse(params string[] rows)
    {
        var map = new GridMap(rows[0].Length, rows.Length);
        for (var y = 0; y < rows.Length; y++)
        {
            for (var x = 0; x < rows[y].Length; x++)
            {
                if (rows[y][x] == '.')
                {
                    map.SetWalkable(x, y, true);
                }
            }
        }

        return map;
    }

    private static GateLayout SingleIslandLayout(params GridPoint[] gates)
    {
        return GateLayout.Create(new IReadOnlyList<GridPoint>[] { gates });
    }

    private static List<(int Target, float Cost)> NeighborsOf(GateGraph graph, int gateId)
    {
        var result = new List<(int, float)>();
        for (var e = graph.EdgeStart[gateId]; e < graph.EdgeStart[gateId + 1]; e++)
        {
            result.Add((graph.EdgeTarget[e], graph.EdgeCost[e]));
        }

        return result;
    }

    [Fact]
    public void Open_island_produces_manhattan_pair_costs()
    {
        var map = Parse(
            ".....",
            ".....",
            ".....",
            ".....");
        var gates = new[] { new GridPoint(0, 0), new GridPoint(4, 0), new GridPoint(4, 3) };
        var layout = SingleIslandLayout(gates);
        var builder = new GateGraphBuilder(layout);

        var added = GatePrecompute.AddIntraIslandEdges(
            builder, layout, 0, map, new GridSearchBuffers(map));
        var graph = builder.Build();

        Assert.Equal(3, added); //* C(3,2)
        var n0 = NeighborsOf(graph, 0);
        Assert.Contains((1, 4f), n0); //* (0,0)->(4,0)
        Assert.Contains((2, 7f), n0); //* (0,0)->(4,3)
        Assert.Contains((2, 3f), NeighborsOf(graph, 1)); //* (4,0)->(4,3)
    }

    [Fact]
    public void Walls_raise_pair_costs_above_manhattan()
    {
        var map = Parse(
            "..#..",
            "..#..",
            "..#..",
            ".....");
        var gates = new[] { new GridPoint(0, 0), new GridPoint(4, 0) };
        var layout = SingleIslandLayout(gates);
        var builder = new GateGraphBuilder(layout);

        GatePrecompute.AddIntraIslandEdges(builder, layout, 0, map, new GridSearchBuffers(map));
        var graph = builder.Build();

        Assert.Equal(new[] { (1, 10f) }, NeighborsOf(graph, 0));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(8)]
    [InlineData(16)]
    public void Fully_connected_island_adds_n_choose_2_edges(int n)
    {
        var map = new GridMap(n + 2, 3, defaultWalkable: true);
        var gates = new GridPoint[n];
        for (var g = 0; g < n; g++)
        {
            gates[g] = new GridPoint(g, 0);
        }

        var layout = SingleIslandLayout(gates);
        var builder = new GateGraphBuilder(layout);

        var added = GatePrecompute.AddIntraIslandEdges(
            builder, layout, 0, map, new GridSearchBuffers(map));
        var graph = builder.Build();

        Assert.Equal(n * (n - 1) / 2, added);
        Assert.Equal(n * (n - 1), graph.EdgeCount);
    }

    [Fact]
    public void Islands_with_fewer_than_two_gates_add_nothing()
    {
        var map = new GridMap(3, 3, defaultWalkable: true);

        var oneGate = SingleIslandLayout(new GridPoint(0, 0));
        var builderOne = new GateGraphBuilder(oneGate);
        Assert.Equal(0, GatePrecompute.AddIntraIslandEdges(
            builderOne, oneGate, 0, map, new GridSearchBuffers(map)));

        var noGates = SingleIslandLayout();
        var builderNone = new GateGraphBuilder(noGates);
        Assert.Equal(0, GatePrecompute.AddIntraIslandEdges(
            builderNone, noGates, 0, map, new GridSearchBuffers(map)));
    }

    [Fact]
    public void Gates_in_disconnected_pockets_get_no_edges()
    {
        var map = Parse(
            "..#..",
            "..#..",
            "..#..");
        var gates = new[] { new GridPoint(0, 0), new GridPoint(0, 2), new GridPoint(4, 1) };
        var layout = SingleIslandLayout(gates);
        var builder = new GateGraphBuilder(layout);

        var added = GatePrecompute.AddIntraIslandEdges(
            builder, layout, 0, map, new GridSearchBuffers(map));
        var graph = builder.Build();

        Assert.Equal(1, added);
        Assert.Equal(2, graph.EdgeCount);
        Assert.Equal(new[] { (1, 2f) }, NeighborsOf(graph, 0));
        Assert.Equal(0, graph.DegreeOf(2));
    }

    [Fact]
    public void Pair_costs_agree_with_FindPath_lengths()
    {
        var map = Parse(
            "......",
            ".##.#.",
            "......",
            ".#.##.",
            "......");
        var gates = new[]
        {
            new GridPoint(0, 0), new GridPoint(5, 0),
            new GridPoint(0, 4), new GridPoint(5, 4),
        };
        var layout = SingleIslandLayout(gates);
        var builder = new GateGraphBuilder(layout);
        var buffers = new GridSearchBuffers(map);

        GatePrecompute.AddIntraIslandEdges(builder, layout, 0, map, buffers);
        var graph = builder.Build();

        Span<int> path = stackalloc int[map.CellCount];
        for (var a = 0; a < gates.Length; a++)
        {
            foreach (var (target, cost) in NeighborsOf(graph, a))
            {
                var length = GridPathfinder.FindPath(
                    map, buffers,
                    map.ToIndex(gates[a]), map.ToIndex(gates[target]), path);

                Assert.Equal(length - 1, cost);
            }
        }
    }

    [Fact]
    public void Span_overload_accepts_precomputed_gate_cells()
    {
        var map = Parse(
            "...",
            "...");
        var layout = SingleIslandLayout(new GridPoint(0, 0), new GridPoint(2, 1));
        var builder = new GateGraphBuilder(layout);
        var gateCells = new[] { map.ToIndex(0, 0), map.ToIndex(2, 1) };

        var added = GatePrecompute.AddIntraIslandEdges(
            builder, 0, map, new GridSearchBuffers(map), gateCells);

        Assert.Equal(1, added);
        Assert.Contains((1, 3f), NeighborsOf(builder.Build(), 0));
    }

    [Fact]
    public void Gate_positions_outside_the_island_map_are_rejected()
    {
        var map = Parse(
            "...",
            "...");
        var layout = SingleIslandLayout(new GridPoint(0, 0), new GridPoint(200, 30));
        var builder = new GateGraphBuilder(layout);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            GatePrecompute.AddIntraIslandEdges(
                builder, layout, 0, map, new GridSearchBuffers(map)));
    }

    [Fact]
    public void Null_arguments_are_rejected()
    {
        var map = Parse("..");
        var layout = SingleIslandLayout(new GridPoint(0, 0), new GridPoint(1, 0));

        Assert.Throws<ArgumentNullException>(() =>
            GatePrecompute.AddIntraIslandEdges(
                null!, layout, 0, map, new GridSearchBuffers(map)));
        Assert.Throws<ArgumentNullException>(() =>
            GatePrecompute.AddIntraIslandEdges(
                new GateGraphBuilder(layout), null!, 0, map, new GridSearchBuffers(map)));
        Assert.Throws<ArgumentNullException>(() =>
            GatePrecompute.AddIntraIslandEdges(
                new GateGraphBuilder(layout), layout, 0, null!, new GridSearchBuffers(map)));
    }
}
