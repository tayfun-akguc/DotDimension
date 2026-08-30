namespace DotDimension.Pathfinding.Tests;

public sealed class StringPullingTests
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

    [Fact]
    public void Open_straight_lines_have_sight()
    {
        var map = Parse(
            "....",
            "....",
            "....");

        Assert.True(StringPulling.HasLineOfSight(map, new GridPoint(0, 0), new GridPoint(3, 0)));
        Assert.True(StringPulling.HasLineOfSight(map, new GridPoint(1, 0), new GridPoint(1, 2)));
        Assert.True(StringPulling.HasLineOfSight(map, new GridPoint(0, 0), new GridPoint(0, 0)));
    }

    [Fact]
    public void A_wall_on_the_line_blocks_sight()
    {
        var map = Parse(
            "..#..",
            ".....");

        Assert.False(StringPulling.HasLineOfSight(map, new GridPoint(0, 0), new GridPoint(4, 0)));
        Assert.True(StringPulling.HasLineOfSight(map, new GridPoint(0, 1), new GridPoint(4, 1)));
    }

    [Fact]
    public void Unwalkable_endpoints_have_no_sight()
    {
        var map = Parse(
            ".#",
            "..");

        Assert.False(StringPulling.HasLineOfSight(map, new GridPoint(1, 0), new GridPoint(0, 1)));
        Assert.False(StringPulling.HasLineOfSight(map, new GridPoint(0, 0), new GridPoint(1, 0)));
    }

    [Fact]
    public void Diagonal_sight_on_open_map_is_clear()
    {
        var map = Parse(
            "..",
            "..");

        Assert.True(StringPulling.HasLineOfSight(map, new GridPoint(0, 0), new GridPoint(1, 1)));
    }

    [Fact]
    public void Corner_crossing_requires_both_side_cells()
    {
        var oneSideBlocked = Parse(
            "..",
            "#.");
        Assert.False(StringPulling.HasLineOfSight(
            oneSideBlocked, new GridPoint(0, 0), new GridPoint(1, 1)));

        var bothSidesBlocked = Parse(
            ".#",
            "#.");
        Assert.False(StringPulling.HasLineOfSight(
            bothSidesBlocked, new GridPoint(0, 0), new GridPoint(1, 1)));
    }

    [Fact]
    public void Center_wall_blocks_the_long_diagonal()
    {
        var map = Parse(
            "...",
            ".#.",
            "...");

        Assert.False(StringPulling.HasLineOfSight(map, new GridPoint(0, 0), new GridPoint(2, 2)));
        Assert.True(StringPulling.HasLineOfSight(map, new GridPoint(0, 0), new GridPoint(2, 0)));
        Assert.True(StringPulling.HasLineOfSight(map, new GridPoint(0, 0), new GridPoint(0, 2)));
    }

    private static int[] SimplifyPath(GridMap map, params GridPoint[] pathPoints)
    {
        var path = pathPoints.Select(map.ToIndex).ToArray();
        Span<int> waypoints = stackalloc int[path.Length];
        var count = StringPulling.Simplify(map, path, waypoints);
        return waypoints[..count].ToArray();
    }

    [Fact]
    public void Straight_corridor_reduces_to_its_endpoints()
    {
        var map = Parse("......");

        var waypoints = SimplifyPath(
            map,
            new GridPoint(0, 0), new GridPoint(1, 0), new GridPoint(2, 0),
            new GridPoint(3, 0), new GridPoint(4, 0), new GridPoint(5, 0));

        Assert.Equal(new[] { map.ToIndex(0, 0), map.ToIndex(5, 0) }, waypoints);
    }

    [Fact]
    public void L_shaped_route_keeps_the_turn_cells()
    {
        var map = Parse(
            ".#.",
            "...");

        var waypoints = SimplifyPath(
            map,
            new GridPoint(0, 0), new GridPoint(0, 1), new GridPoint(1, 1),
            new GridPoint(2, 1), new GridPoint(2, 0));

        Assert.Equal(
            new[] { map.ToIndex(0, 0), map.ToIndex(0, 1), map.ToIndex(2, 1), map.ToIndex(2, 0) },
            waypoints);
    }

    [Fact]
    public void Trivial_paths_pass_through()
    {
        var map = Parse("...");

        Assert.Equal(0, StringPulling.Simplify(map, ReadOnlySpan<int>.Empty, stackalloc int[1]));

        Span<int> one = stackalloc int[1];
        Assert.Equal(1, StringPulling.Simplify(map, stackalloc int[] { 1 }, one));
        Assert.Equal(1, one[0]);

        Span<int> two = stackalloc int[2];
        Assert.Equal(2, StringPulling.Simplify(map, stackalloc int[] { 0, 1 }, two));
        Assert.Equal(new[] { 0, 1 }, two.ToArray());
    }

    [Fact]
    public void Simplified_route_honors_the_three_contracts()
    {
        var map = Parse(
            "......",
            ".##.#.",
            "......",
            ".#.##.",
            "......");
        var buffers = new GridSearchBuffers(map);
        Span<int> path = stackalloc int[map.CellCount];
        var length = GridPathfinder.FindPath(
            map, buffers, map.ToIndex(0, 0), map.ToIndex(5, 4), path);
        Assert.True(length > 0);

        Span<int> waypoints = stackalloc int[length];
        var count = StringPulling.Simplify(map, path[..length], waypoints);

        Assert.True(count >= 2);
        Assert.True(count <= length);
        Assert.Equal(path[0], waypoints[0]);
        Assert.Equal(path[length - 1], waypoints[count - 1]);

        var polyline = 0.0;
        for (var i = 1; i < count; i++)
        {
            Assert.True(StringPulling.HasLineOfSight(map, waypoints[i - 1], waypoints[i]));

            var a = map.ToPoint(waypoints[i - 1]);
            var b = map.ToPoint(waypoints[i]);
            polyline += Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
        }

        Assert.True(polyline <= length - 1);
    }

    [Fact]
    public void Too_small_waypoint_span_throws()
    {
        var map = Parse("...");
        var path = new[] { 0, 1, 2 };

        Assert.Throws<ArgumentException>(() =>
            StringPulling.Simplify(map, path, new int[1]));
    }

    [Fact]
    public void Out_of_range_cells_are_rejected()
    {
        var map = Parse("...");

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            StringPulling.HasLineOfSight(map, 0, 99));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            StringPulling.HasLineOfSight(map, -1, 0));
    }
}
