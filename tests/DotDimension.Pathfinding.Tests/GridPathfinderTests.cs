namespace DotDimension.Pathfinding.Tests;

public sealed class GridPathfinderTests
{
    private static GridMap Parse(params string[] rows)
    {
        var height = rows.Length;
        var width = rows[0].Length;
        var map = new GridMap(width, height);
        for (var y = 0; y < height; y++)
        {
            Assert.Equal(width, rows[y].Length);
            for (var x = 0; x < width; x++)
            {
                if (rows[y][x] == '.')
                {
                    map.SetWalkable(x, y, true);
                }
            }
        }

        return map;
    }

    private static int[] FindPath(GridMap map, int start, int goal, GridSearchBuffers? buffers = null)
    {
        buffers ??= new GridSearchBuffers(map);
        Span<int> outPath = stackalloc int[map.CellCount];
        var length = GridPathfinder.FindPath(map, buffers, start, goal, outPath);
        return length == GridPathfinder.NoPath
            ? Array.Empty<int>()
            : outPath[..length].ToArray();
    }

    [Fact]
    public void Straight_line_path_on_open_map()
    {
        var map = Parse(
            ".....",
            ".....",
            ".....");
        var start = map.ToIndex(0, 1);
        var goal = map.ToIndex(4, 1);

        var path = FindPath(map, start, goal);

        Assert.Equal(5, path.Length);
        Assert.Equal(start, path[0]);
        Assert.Equal(goal, path[^1]);
    }

    [Fact]
    public void Start_equals_goal_returns_single_cell_path()
    {
        var map = Parse("...", "...");
        var cell = map.ToIndex(1, 1);

        var path = FindPath(map, cell, cell);

        Assert.Equal(new[] { cell }, path);
    }

    [Fact]
    public void Every_step_in_the_path_is_walkable_and_adjacent()
    {
        var map = Parse(
            "......",
            ".##...",
            "......",
            "...##.",
            "......");
        var path = FindPath(map, map.ToIndex(0, 0), map.ToIndex(5, 4));

        Assert.NotEmpty(path);
        foreach (var cell in path)
        {
            Assert.True(map.IsWalkable(cell));
        }

        for (var i = 1; i < path.Length; i++)
        {
            var a = map.ToPoint(path[i - 1]);
            var b = map.ToPoint(path[i]);
            var manhattan = Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
            Assert.Equal(1, manhattan);
        }
    }

    [Fact]
    public void Path_routes_around_a_wall()
    {
        var map = Parse(
            "..#..",
            "..#..",
            "..#..",
            ".....");
        var start = map.ToIndex(0, 0);
        var goal = map.ToIndex(4, 0);

        var path = FindPath(map, start, goal);


        Assert.Equal(11, path.Length);
        Assert.Contains(map.ToIndex(2, 3), path);
    }

    [Fact]
    public void Unreachable_goal_returns_NoPath()
    {
        var map = Parse(
            "..#..",
            "..#..",
            "..#..");

        var path = FindPath(map, map.ToIndex(0, 1), map.ToIndex(4, 1));

        Assert.Empty(path);
    }

    [Fact]
    public void Unwalkable_start_or_goal_returns_NoPath()
    {
        var map = Parse(
            "...",
            ".#.",
            "...");
        var wall = map.ToIndex(1, 1);
        var open = map.ToIndex(0, 0);

        Assert.Empty(FindPath(map, wall, open));
        Assert.Empty(FindPath(map, open, wall));
    }

    [Fact]
    public void Works_on_non_square_maps_without_row_wrapping()
    {
        var map = Parse(
            ".#",
            "..",
            "..");
        var path = FindPath(map, map.ToIndex(1, 2), map.ToIndex(0, 0));

        Assert.Equal(4, path.Length);
        Assert.DoesNotContain(map.ToIndex(1, 0), path);
    }

    [Fact]
    public void Path_length_is_optimal_on_maze_with_two_routes()
    {
        var map = Parse(
            ".....",
            ".###.",
            ".....",
            ".....");
        var start = map.ToIndex(0, 1);
        var goal = map.ToIndex(4, 1);

        var path = FindPath(map, start, goal);

        Assert.Equal(7, path.Length);
    }

    [Fact]
    public void Same_buffers_serve_many_searches_with_identical_results()
    {
        var map = Parse(
            "......",
            ".####.",
            "......");
        var buffers = new GridSearchBuffers(map);
        var start = map.ToIndex(0, 1);
        var goal = map.ToIndex(5, 1);

        var first = FindPath(map, start, goal, buffers);

        FindPath(map, map.ToIndex(0, 0), map.ToIndex(5, 2), buffers);
        FindPath(map, goal, start, buffers);

        var again = FindPath(map, start, goal, buffers);

        Assert.Equal(first, again);
    }

    [Fact]
    public void Oversized_buffers_work_for_smaller_maps()
    {
        var big = new GridMap(50, 50);
        var buffers = new GridSearchBuffers(big);

        var small = Parse(
            "...",
            "...");
        Assert.True(buffers.Fits(small));

        var path = FindPath(small, small.ToIndex(0, 0), small.ToIndex(2, 1), buffers);
        Assert.Equal(4, path.Length);
    }

    [Fact]
    public void Undersized_buffers_are_rejected()
    {
        var map = Parse(
            "....",
            "....");
        var buffers = new GridSearchBuffers(4);

        Assert.Throws<ArgumentException>(() =>
            GridPathfinder.FindPath(map, buffers, 0, 7, new int[8]));
    }

    [Fact]
    public void FindCosts_returns_optimal_costs_for_all_targets()
    {
        var map = Parse(
            ".....",
            ".....",
            ".....");
        var start = map.ToIndex(0, 0);
        var targets = new[] { map.ToIndex(4, 0), map.ToIndex(0, 2), map.ToIndex(4, 2) };
        var costs = new float[targets.Length];
        var buffers = new GridSearchBuffers(map);

        var reached = GridPathfinder.FindCosts(map, buffers, start, targets, costs);

        Assert.Equal(3, reached);
        Assert.Equal(4f, costs[0]);
        Assert.Equal(2f, costs[1]);
        Assert.Equal(6f, costs[2]);
    }

    [Fact]
    public void FindCosts_marks_unreachable_targets_with_infinity()
    {
        var map = Parse(
            "..#..",
            "..#..",
            "..#..");
        var start = map.ToIndex(0, 1);
        var targets = new[] { map.ToIndex(1, 0), map.ToIndex(4, 1) };
        var costs = new float[2];

        var reached = GridPathfinder.FindCosts(
            map, new GridSearchBuffers(map), start, targets, costs);

        Assert.Equal(1, reached);
        Assert.Equal(2f, costs[0]);
        Assert.True(float.IsPositiveInfinity(costs[1]));
    }

    [Fact]
    public void FindCosts_agrees_with_FindPath_lengths()
    {
        var map = Parse(
            "......",
            ".##.#.",
            "......",
            ".#.##.",
            "......");
        var buffers = new GridSearchBuffers(map);
        var start = map.ToIndex(0, 0);
        var targets = new[]
        {
            map.ToIndex(5, 0), map.ToIndex(5, 4), map.ToIndex(2, 2), map.ToIndex(0, 4),
        };
        var costs = new float[targets.Length];

        GridPathfinder.FindCosts(map, buffers, start, targets, costs);

        for (var t = 0; t < targets.Length; t++)
        {
            var path = FindPath(map, start, targets[t], buffers);
            Assert.Equal(path.Length - 1, costs[t]);
        }
    }

    [Fact]
    public void FindCosts_with_unwalkable_start_reaches_nothing()
    {
        var map = Parse(
            "...",
            ".#.");
        var targets = new[] { map.ToIndex(0, 0) };
        var costs = new float[1];

        var reached = GridPathfinder.FindCosts(
            map, new GridSearchBuffers(map), map.ToIndex(1, 1), targets, costs);

        Assert.Equal(0, reached);
        Assert.True(float.IsPositiveInfinity(costs[0]));
    }

    [Fact]
    public void FindCosts_rejects_mismatched_span_lengths()
    {
        var map = Parse("...");

        Assert.Throws<ArgumentException>(() =>
            GridPathfinder.FindCosts(
                map, new GridSearchBuffers(map), 0, new[] { 1, 2 }, new float[1]));
    }
}
