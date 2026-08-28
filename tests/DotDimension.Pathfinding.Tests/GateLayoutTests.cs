namespace DotDimension.Pathfinding.Tests;

public sealed class GateLayoutTests
{
    //* four-island example: A(3), B(5), C(4), D(2) -> prefix [0, 3, 8, 12, 14].
    private static GateLayout CreateSampleLayout()
    {
        return GateLayout.Create(new[]
        {
            Gates((10, 0), (10, 1), (10, 2)),
            Gates((20, 0), (20, 1), (20, 2), (20, 3), (20, 4)),
            Gates((30, 0), (30, 1), (30, 2), (30, 3)),
            Gates((40, 0), (40, 1)),
        });
    }

    private static IReadOnlyList<GridPoint> Gates(params (int X, int Y)[] points)
    {
        return points.Select(p => new GridPoint(p.X, p.Y)).ToArray();
    }

    [Fact]
    public void Counts_follow_the_input_lists()
    {
        var layout = CreateSampleLayout();

        Assert.Equal(4, layout.IslandCount);
        Assert.Equal(14, layout.GateCount);
        Assert.Equal(3, layout.GateCountOf(0));
        Assert.Equal(5, layout.GateCountOf(1));
        Assert.Equal(4, layout.GateCountOf(2));
        Assert.Equal(2, layout.GateCountOf(3));
    }

    [Fact]
    public void FirstGateOf_returns_prefix_starts()
    {
        var layout = CreateSampleLayout();

        Assert.Equal(0, layout.FirstGateOf(0));
        Assert.Equal(3, layout.FirstGateOf(1));
        Assert.Equal(8, layout.FirstGateOf(2));
        Assert.Equal(12, layout.FirstGateOf(3));
    }

    [Fact]
    public void GateId_maps_island_and_local_to_global()
    {
        var layout = CreateSampleLayout();

        Assert.Equal(0, layout.GateId(0, 0));
        Assert.Equal(7, layout.GateId(1, 4));
        Assert.Equal(9, layout.GateId(2, 1));
        Assert.Equal(13, layout.GateId(3, 1));
    }

    [Fact]
    public void IslandOf_finds_owner_by_range()
    {
        var layout = CreateSampleLayout();

        Assert.Equal(0, layout.IslandOf(0));
        Assert.Equal(0, layout.IslandOf(2));
        Assert.Equal(1, layout.IslandOf(3));
        Assert.Equal(2, layout.IslandOf(9));
        Assert.Equal(3, layout.IslandOf(12));
        Assert.Equal(3, layout.IslandOf(13));
    }

    [Fact]
    public void Id_conversions_round_trip_every_gate()
    {
        var layout = CreateSampleLayout();

        for (var island = 0; island < layout.IslandCount; island++)
        {
            for (var local = 0; local < layout.GateCountOf(island); local++)
            {
                var gateId = layout.GateId(island, local);

                Assert.Equal(island, layout.IslandOf(gateId));
                Assert.Equal(local, layout.LocalGateOf(gateId));
            }
        }
    }

    [Fact]
    public void PositionOf_returns_the_input_points_in_order()
    {
        var layout = CreateSampleLayout();

        Assert.Equal(new GridPoint(10, 0), layout.PositionOf(0));
        Assert.Equal(new GridPoint(20, 4), layout.PositionOf(7));
        Assert.Equal(new GridPoint(40, 1), layout.PositionOf(13));
    }

    [Fact]
    public void Zero_gate_islands_are_supported_and_skipped_in_ownership()
    {
        var layout = GateLayout.Create(new[]
        {
            Gates((0, 0), (0, 1)),
            Gates(),
            Gates(),
            Gates((5, 0), (5, 1), (5, 2)),
        });

        Assert.Equal(4, layout.IslandCount);
        Assert.Equal(5, layout.GateCount);
        Assert.Equal(0, layout.GateCountOf(1));
        Assert.Equal(0, layout.GateCountOf(2));

        Assert.Equal(0, layout.IslandOf(1));
        Assert.Equal(3, layout.IslandOf(2));
        Assert.Equal(0, layout.LocalGateOf(2));
        Assert.Equal(3, layout.IslandOf(4));
    }

    [Fact]
    public void Layout_with_no_gates_is_valid()
    {
        var layout = GateLayout.Create(new[] { Gates(), Gates() });

        Assert.Equal(2, layout.IslandCount);
        Assert.Equal(0, layout.GateCount);
        Assert.Equal(0, layout.GateCountOf(0));
    }

    [Fact]
    public void Create_rejects_null_input()
    {
        Assert.Throws<ArgumentNullException>(() => GateLayout.Create(null!));
        Assert.Throws<ArgumentException>(() =>
            GateLayout.Create(new IReadOnlyList<GridPoint>[] { null! }));
    }

    [Fact]
    public void Island_indexed_members_reject_out_of_range_islands()
    {
        var layout = CreateSampleLayout();

        Assert.Throws<ArgumentOutOfRangeException>(() => layout.GateCountOf(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => layout.GateCountOf(4));
        Assert.Throws<ArgumentOutOfRangeException>(() => layout.FirstGateOf(4));
        Assert.Throws<ArgumentOutOfRangeException>(() => layout.GateId(4, 0));
    }

    [Fact]
    public void GateId_rejects_local_gates_the_island_does_not_have()
    {
        var layout = CreateSampleLayout();

        Assert.Throws<ArgumentOutOfRangeException>(() => layout.GateId(0, 3));
        Assert.Throws<ArgumentOutOfRangeException>(() => layout.GateId(3, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => layout.GateId(0, -1));
    }

    [Fact]
    public void Gate_indexed_members_reject_out_of_range_ids()
    {
        var layout = CreateSampleLayout();

        Assert.Throws<ArgumentOutOfRangeException>(() => layout.IslandOf(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => layout.IslandOf(14));
        Assert.Throws<ArgumentOutOfRangeException>(() => layout.PositionOf(14));
    }

    [Fact]
    public void Round_trip_holds_on_large_layout()
    {
        var islands = new List<IReadOnlyList<GridPoint>>();
        for (var i = 0; i < 1000; i++)
        {
            var count = i % 17;
            var gates = new GridPoint[count];
            for (var g = 0; g < count; g++)
            {
                gates[g] = new GridPoint(i, g);
            }

            islands.Add(gates);
        }

        var layout = GateLayout.Create(islands);

        Assert.Equal(1000, layout.IslandCount);
        for (var island = 0; island < layout.IslandCount; island++)
        {
            Assert.Equal(island % 17, layout.GateCountOf(island));
            for (var local = 0; local < layout.GateCountOf(island); local++)
            {
                var id = layout.GateId(island, local);
                Assert.Equal(island, layout.IslandOf(id));
                Assert.Equal(local, layout.LocalGateOf(id));
                Assert.Equal(new GridPoint(island, local), layout.PositionOf(id));
            }
        }
    }
}
