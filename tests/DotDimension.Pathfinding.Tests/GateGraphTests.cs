namespace DotDimension.Pathfinding.Tests;

public sealed class GateGraphTests
{
    private static IReadOnlyList<GridPoint> Gates(int count, int islandTag)
    {
        var points = new GridPoint[count];
        for (var g = 0; g < count; g++)
        {
            points[g] = new GridPoint(islandTag, g);
        }

        return points;
    }

    private static GateLayout SampleLayout()
    {
        return GateLayout.Create(new[]
        {
            Gates(3, 10), Gates(5, 20), Gates(4, 30), Gates(2, 40),
        });
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
    public void Empty_graph_has_no_edges_and_all_zero_degrees()
    {
        var graph = new GateGraphBuilder(SampleLayout()).Build();

        Assert.Equal(14, graph.NodeCount);
        Assert.Equal(0, graph.EdgeCount);
        Assert.Equal(15, graph.EdgeStart.Length);
        for (var g = 0; g < graph.NodeCount; g++)
        {
            Assert.Equal(0, graph.DegreeOf(g));
        }
    }

    [Fact]
    public void Single_edge_produces_two_directed_records()
    {
        var builder = new GateGraphBuilder(SampleLayout());
        builder.AddIntraIslandEdge(0, localGateA: 0, localGateB: 2, cost: 7f);

        var graph = builder.Build();

        Assert.Equal(2, graph.EdgeCount);
        Assert.Equal(new[] { (2, 7f) }, NeighborsOf(graph, 0));
        Assert.Equal(new[] { (0, 7f) }, NeighborsOf(graph, 2));
        Assert.Equal(0, graph.DegreeOf(1));
    }

    [Fact]
    public void Neighbors_are_contiguous_and_costs_stay_paired()
    {
        var layout = SampleLayout();
        var builder = new GateGraphBuilder(layout);
        builder.AddIntraIslandEdge(1, 0, 1, 5f);
        builder.AddIntraIslandEdge(1, 0, 2, 9f);
        builder.AddIntraIslandEdge(1, 1, 2, 4f);
        builder.AddInterIslandEdge(0, 2, 1, 0);

        var graph = builder.Build();

        Assert.Equal(8, graph.EdgeCount);

        var b0 = NeighborsOf(graph, 3);
        Assert.Equal(3, graph.DegreeOf(3));
        Assert.Contains((4, 5f), b0);
        Assert.Contains((5, 9f), b0);
        Assert.Contains((2, 1f), b0);

        Assert.Equal(new[] { (3, 1f) }, NeighborsOf(graph, 2));
        Assert.Equal(2, graph.DegreeOf(4));
        Assert.Equal(2, graph.DegreeOf(5));

        Assert.Equal(graph.EdgeCount, graph.EdgeStart[^1]);
    }

    [Fact]
    public void Duplicate_edges_are_keep_as_parallel_records()
    {
        var builder = new GateGraphBuilder(SampleLayout());
        builder.AddIntraIslandEdge(1, 0, 1, 5f);
        builder.AddIntraIslandEdge(1, 0, 1, 3f);

        var graph = builder.Build();

        Assert.Equal(4, graph.EdgeCount);
        var neighbors = NeighborsOf(graph, 3);
        Assert.Equal(2, neighbors.Count);
        Assert.Contains((4, 5f), neighbors);
        Assert.Contains((4, 3f), neighbors);
    }

    [Fact]
    public void Gates_of_decorative_islands_stay_out_of_graph()
    {
        var layout = GateLayout.Create(new[]
        {
            Gates(2, 10),
            Gates(4, 20),
        });
        var builder = new GateGraphBuilder(layout);
        builder.AddIntraIslandEdge(0, 0, 1, 2f);

        var graph = builder.Build();

        Assert.Equal(6, graph.NodeCount);
        for (var g = 2; g <= 5; g++)
        {
            Assert.Equal(0, graph.DegreeOf(g));
        }
    }


    [Fact]
    public void Csr_arrays_match_five_node_example()
    {
        var layout = GateLayout.Create(new[]
        {
            Gates(1, 0), Gates(1, 1), Gates(1, 2), Gates(1, 3), Gates(1, 4),
        });
        var builder = new GateGraphBuilder(layout);
        builder.AddEdge(0, 1, 4f);
        builder.AddEdge(0, 3, 7f);
        builder.AddEdge(1, 2, 2f);
        builder.AddEdge(1, 3, 5f);
        builder.AddEdge(1, 4, 9f);
        builder.AddEdge(2, 4, 3f);
        builder.AddEdge(3, 4, 1f);

        var graph = builder.Build();

        Assert.Equal(14, graph.EdgeCount);
        Assert.Equal(2, graph.DegreeOf(0));
        Assert.Equal(4, graph.DegreeOf(1));
        Assert.Equal(2, graph.DegreeOf(2));
        Assert.Equal(3, graph.DegreeOf(3));
        Assert.Equal(3, graph.DegreeOf(4));

        var n1 = NeighborsOf(graph, 1);
        Assert.Contains((0, 4f), n1);
        Assert.Contains((2, 2f), n1);
        Assert.Contains((3, 5f), n1);
        Assert.Contains((4, 9f), n1);

        var n4 = NeighborsOf(graph, 4);
        Assert.Contains((1, 9f), n4);
        Assert.Contains((2, 3f), n4);
        Assert.Contains((3, 1f), n4);

        var degreeSum = 0;
        for (var g = 0; g < graph.NodeCount; g++) degreeSum += graph.DegreeOf(g);
        Assert.Equal(graph.EdgeCount, degreeSum);
    }

    [Fact]
    public void Edges_to_nonexistent_gates_fail()
    {
        var builder = new GateGraphBuilder(SampleLayout());

        Assert.Throws<ArgumentOutOfRangeException>(() => builder.AddIntraIslandEdge(0, 0, 3, 1f));
        Assert.Throws<ArgumentOutOfRangeException>(() => builder.AddIntraIslandEdge(4, 0, 0, 1f));
        Assert.Throws<ArgumentOutOfRangeException>(() => builder.AddInterIslandEdge(0, 0, 3, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => builder.AddEdge(0, 14, 1f));
    }

    [Fact]
    public void Self_loops_are_rejected()
    {
        var builder = new GateGraphBuilder(SampleLayout());

        Assert.Throws<ArgumentException>(() => builder.AddEdge(5, 5, 1f));
        Assert.Throws<ArgumentException>(() => builder.AddIntraIslandEdge(1, 2, 2, 1f));
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NaN)]
    public void Non_positive_or_non_finite_costs_are_rejected(float cost)
    {
        var builder = new GateGraphBuilder(SampleLayout());

        Assert.Throws<ArgumentOutOfRangeException>(() => builder.AddIntraIslandEdge(0, 0, 1, cost));
    }

    [Fact]
    public void Builder_is_single_use()
    {
        var builder = new GateGraphBuilder(SampleLayout());
        builder.AddIntraIslandEdge(0, 0, 1, 2f);
        builder.Build();

        Assert.Throws<InvalidOperationException>(() => builder.Build());
        Assert.Throws<InvalidOperationException>(() => builder.AddIntraIslandEdge(0, 0, 2, 3f));
    }

    [Fact]
    public void Builder_rejects_null_layout()
    {
        Assert.Throws<ArgumentNullException>(() => new GateGraphBuilder(null!));
    }

    [Fact]
    public void DegreeOf_rejects_out_of_range_ids()
    {
        var graph = new GateGraphBuilder(SampleLayout()).Build();

        Assert.Throws<ArgumentOutOfRangeException>(() => graph.DegreeOf(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => graph.DegreeOf(14));
    }

    [Fact]
    public void Graph_exposes_its_layout()
    {
        var layout = SampleLayout();
        var graph = new GateGraphBuilder(layout).Build();

        Assert.Same(layout, graph.Layout);
        Assert.Equal(layout.GateCount, graph.NodeCount);
    }
}
