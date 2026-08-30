namespace DotDimension.Pathfinding.Tests;

public sealed class PathStitcherTests
{
    private static GridPoint P(int x, int y) => new(x, y);

    [Fact]
    public void First_segment_is_copied_as_is()
    {
        Span<GridPoint> buffer = stackalloc GridPoint[8];
        var stitcher = new PathStitcher(buffer);

        stitcher.Append(new[] { P(0, 0), P(1, 0), P(2, 0) });

        Assert.Equal(3, stitcher.Count);
        Assert.Equal(new[] { P(0, 0), P(1, 0), P(2, 0) }, stitcher.Path.ToArray());
    }

    [Fact]
    public void Empty_segments_are_no_ops()
    {
        Span<GridPoint> buffer = stackalloc GridPoint[4];
        var stitcher = new PathStitcher(buffer);

        stitcher.Append(ReadOnlySpan<GridPoint>.Empty);
        Assert.Equal(0, stitcher.Count);

        stitcher.Append(new[] { P(1, 1) });
        stitcher.Append(ReadOnlySpan<GridPoint>.Empty);
        Assert.Equal(1, stitcher.Count);
    }

    [Fact]
    public void Shared_junction_cell_is_written_once()
    {
        Span<GridPoint> buffer = stackalloc GridPoint[8];
        var stitcher = new PathStitcher(buffer);

        stitcher.Append(new[] { P(0, 0), P(3, 0) });
        stitcher.Append(new[] { P(3, 0), P(3, 4), P(5, 4) });

        Assert.Equal(new[] { P(0, 0), P(3, 0), P(3, 4), P(5, 4) }, stitcher.Path.ToArray());
    }

    [Fact]
    public void Adjacent_junction_keeps_both_cells()
    {
        Span<GridPoint> buffer = stackalloc GridPoint[8];
        var stitcher = new PathStitcher(buffer);

        stitcher.Append(new[] { P(10, 3), P(12, 3) });
        stitcher.Append(new[] { P(13, 3), P(15, 3) });

        Assert.Equal(new[] { P(10, 3), P(12, 3), P(13, 3), P(15, 3) }, stitcher.Path.ToArray());
    }

    [Fact]
    public void Teleporting_junction_throws()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
        {
            Span<GridPoint> buffer = stackalloc GridPoint[8];
            var stitcher = new PathStitcher(buffer);
            stitcher.Append(new[] { P(0, 0), P(2, 0) });
            stitcher.Append(new[] { P(5, 5), P(6, 5) });
        });

        Assert.Contains("(2, 0)", exception.Message);
        Assert.Contains("(5, 5)", exception.Message);
    }

    [Fact]
    public void Interior_waypoint_jumps_are_allowed()
    {
        Span<GridPoint> buffer = stackalloc GridPoint[4];
        var stitcher = new PathStitcher(buffer);

        stitcher.Append(new[] { P(0, 0), P(40, 0), P(40, 25) });

        Assert.Equal(3, stitcher.Count);
    }

    [Fact]
    public void Local_segments_are_translated_by_the_island_origin()
    {
        Span<GridPoint> buffer = stackalloc GridPoint[4];
        var stitcher = new PathStitcher(buffer);

        stitcher.Append(new[] { P(0, 0), P(1, 0) }, islandOrigin: P(128, 256));

        Assert.Equal(new[] { P(128, 256), P(129, 256) }, stitcher.Path.ToArray());
    }

    [Fact]
    public void Junction_check_runs_in_world_space()
    {
        Span<GridPoint> buffer = stackalloc GridPoint[8];
        var stitcher = new PathStitcher(buffer);

        stitcher.Append(new[] { P(120, 60), P(127, 60) });
        stitcher.Append(new[] { P(0, 60), P(9, 60) }, islandOrigin: P(128, 0));

        Assert.Equal(
            new[] { P(120, 60), P(127, 60), P(128, 60), P(137, 60) },
            stitcher.Path.ToArray());
    }

    [Fact]
    public void Overflowing_the_destination_buffer_throws()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            Span<GridPoint> buffer = stackalloc GridPoint[2];
            var stitcher = new PathStitcher(buffer);
            stitcher.Append(new[] { P(0, 0), P(1, 0), P(2, 0) });
        });
    }

    [Fact]
    public void Full_route_stitches_castle_paths_cached_segments_and_crossings()
    {
        Span<GridPoint> buffer = stackalloc GridPoint[16];
        var stitcher = new PathStitcher(buffer);

        stitcher.Append(new[] { P(2, 2), P(7, 2) });
        stitcher.Append(new[] { P(0, 2), P(0, 5) }, islandOrigin: P(8, 0));
        stitcher.Append(new[] { P(0, 5), P(3, 5), P(3, 3) }, islandOrigin: P(8, 0));

        var route = stitcher.Path.ToArray();
        Assert.Equal(
            new[] { P(2, 2), P(7, 2), P(8, 2), P(8, 5), P(11, 5), P(11, 3) },
            route);

        Assert.Equal(P(2, 2), route[0]);
        Assert.Equal(P(11, 3), route[^1]);
    }
}
