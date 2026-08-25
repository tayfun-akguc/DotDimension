namespace DotDimension.Pathfinding.Tests;

public sealed class GridMapTests
{

    [Theory]
    [InlineData(0, 5)]
    [InlineData(5, 0)]
    [InlineData(-1, 5)]
    [InlineData(5, -3)]
    public void Constructor_rejects_non_positive_dimensions(int width, int height)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new GridMap(width, height));
    }

    [Theory]
    [InlineData(1, 1, 1)] //* 1 cell - 1 word
    [InlineData(8, 8, 1)] //* 64 cells - exactly 1 word
    [InlineData(13, 5, 2)] //* 65 cells - spill into the second word
    [InlineData(10, 10, 2)] //* 100 cells - 2 words
    [InlineData(128, 128, 256)] //* 16,384 cells - 256 words
    [InlineData(510, 510, 4065)] //* 260_100 cells- 4065 words
    public void Word_count_is_cell_count_divided_by_64_rounded_up(int width, int height, int expectedWords)
    {
        var map = new GridMap(width, height);

        Assert.Equal(expectedWords, map.Bits.Length);
    }

    [Fact]
    public void New_map_is_fully_unwalkable_by_default()
    {
        var map = new GridMap(7, 9);

        for (var i = 0; i < map.CellCount; i++)
        {
            Assert.False(map.IsWalkable(i));
        }
    }

    [Fact]
    public void DefaultWalkable_true_makes_every_cell_walkable()
    {
        var map = new GridMap(7, 9);
        var walkableMap = new GridMap(7, 9, defaultWalkable: true);

        for (var i = 0; i < walkableMap.CellCount; i++)
        {
            Assert.True(walkableMap.IsWalkable(i));
        }

        Assert.Equal(map.Bits.Length, walkableMap.Bits.Length);
    }

    [Fact]
    public void DefaultWalkable_true_clears_unused_tail_bits()
    {
        //* 65 cells
        //* 65 / 64 = 1.015625 = 2 words
        //* first word uses all bits (64 bits allocated, uses 64 bit, 0 wasted)
        //* second word uses 1 bit (64 allocated, uses 1 bit, 63 wasted)
        var map = new GridMap(13, 5, defaultWalkable: true);

        Assert.Equal(2, map.Bits.Length);
        Assert.Equal(ulong.MaxValue, map.Bits[0]); //* 2^64 - 1
        Assert.Equal(1UL, map.Bits[1]);
    }

    //* Bit get, set

    [Fact]
    public void SetWalkable_affects_only_the_targeted_cell()
    {
        var map = new GridMap(10, 10);

        map.SetWalkable(37, true);

        for (var i = 0; i < map.CellCount; i++)
        {
            Assert.Equal(i == 37, map.IsWalkable(i));
        }
    }

    [Fact]
    public void SetWalkable_false_clears_a_previously_set_cell()
    {
        var map = new GridMap(10, 10, defaultWalkable: true);

        map.SetWalkable(4, 2, false);

        Assert.False(map.IsWalkable(4, 2));
        Assert.True(map.IsWalkable(3, 2));
        Assert.True(map.IsWalkable(5, 2));
    }

    [Fact]
    public void Cells_across_word_boundaries_are_independent()
    {
        //* cells 63 and 64 live in different words
        //* cell = wordIndex * 64 + bitIndex
        //* 63 is in the first word, 64 is in the second word
        //* flipping one must not leak.
        //* 13 * 10 = 130 cells = 3 words
        var map = new GridMap(13, 10);

        map.SetWalkable(63, true);

        /*
         * first word
         * bit:     0, 1, 2, ... 62 | 63
         * cell:    0, 1, 2, ... 62 | 63
         *
         * second word
         * bit:      0,  1, 2, ...   62 | 63
         * cell:    64, 65, 66, ... 126 | 127
         *
         * map.SetWalkable(63, true); => changes the 63th bit in the first word
         * cell 63 is now walkable, cell 64 is still not walkable
         */
        Assert.Equal(3, map.Bits.Length);
        Assert.True(map.IsWalkable(63));
        Assert.False(map.IsWalkable(62));
        Assert.False(map.IsWalkable(64));
        Assert.Equal(1UL << 63, map.Bits[0]); //* only the 63th bit is walkable
        Assert.Equal(0UL, map.Bits[1]); //* the other bits are not walkable
        Assert.Equal(0UL, map.Bits[2]); //* the other bits are not walkable
    }

    //* index and point conversions

    [Fact]
    public void ToIndex_uses_row_major_order()
    {
        var map = new GridMap(7, 3);
        /*
         * x0, x1, x2, x3, x4, x5, x6
         * --------------------------
         * 00, 01, 02, 03, 04, 05, 06 | y0
         * 07, 08, 09, 10, 11, 12, 13 | y1
         * 14, 15, 16, 17, 18, 19, 20 | y2
         */

        Assert.Equal(0, map.ToIndex(0, 0));
        Assert.Equal(6, map.ToIndex(6, 0)); //* end of the first row
        Assert.Equal(7, map.ToIndex(0, 1)); //* start of the second row
        Assert.Equal(7 * 2 + 4, map.ToIndex(4, 2));
    }

    [Fact]
    public void ToIndex_and_ToPoint_round_trip_every_cell()
    {
        var map = new GridMap(5, 4); //* non square
        //* ToPoint(ToIndex(p)) == p

        for (var y = 0; y < map.Height; y++)
        {
            for (var x = 0; x < map.Width; x++)
            {
                var index = map.ToIndex(x, y);
                var point = map.ToPoint(index);

                Assert.Equal(new GridPoint(x, y), point);
                Assert.Equal(index, map.ToIndex(point));
            }
        }
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(5, 0)]
    [InlineData(0, 4)]
    public void ToIndex_rejects_out_of_bounds_coordinates(int x, int y)
    {
        var map = new GridMap(5, 4);

        Assert.Throws<ArgumentOutOfRangeException>(() => map.ToIndex(x, y));
    }

    [Fact]
    public void InBounds_matches_map_dimensions()
    {
        var map = new GridMap(5, 4);

        Assert.True(map.InBounds(0, 0));
        Assert.True(map.InBounds(4, 3));
        Assert.False(map.InBounds(5, 0));
        Assert.False(map.InBounds(0, 4));
        Assert.False(map.InBounds(-1, 0));
        Assert.False(map.InBounds(0, -1));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(20)]
    public void Accessors_reject_out_of_range_cell_index(int cellIndex)
    {
        var map = new GridMap(5, 4); //* 20 cells: valid indices are [0,19]

        Assert.Throws<ArgumentOutOfRangeException>(() => map.IsWalkable(cellIndex));
        Assert.Throws<ArgumentOutOfRangeException>(() => map.SetWalkable(cellIndex, true));
        Assert.Throws<ArgumentOutOfRangeException>(() => map.ToPoint(cellIndex));
    }

    //* FromBools
    [Fact]
    public void FromBools_uses_x_y_indexing_convention()
    {
        //* walkable[x, y]: GetLength(0) = Width = 3, GetLength(1) = Height = 2.
        var walkable = new bool[3, 2];
        walkable[2, 0] = true; //* (x=2, y=0)

        var map = GridMap.FromBools(walkable);

        Assert.Equal(3, map.Width);
        Assert.Equal(2, map.Height);
        Assert.True(map.IsWalkable(2, 0));
        Assert.False(map.IsWalkable(0, 2 % map.Height));
        for (var i = 0; i < map.CellCount; i++)
        {
            Assert.Equal(i == map.ToIndex(2, 0), map.IsWalkable(i));
        }
    }

    [Fact]
    public void FromBools_rejects_null()
    {
        Assert.Throws<ArgumentNullException>(() => GridMap.FromBools(null!));
    }

    [Fact]
    public void FromBits_round_trips_map_content()
    {
        var original = new GridMap(13, 5); //* 65 cells, 2 words
        original.SetWalkable(0, true);
        original.SetWalkable(63, true);
        original.SetWalkable(64, true);

        var restored = GridMap.FromBits(original.Width, original.Height, original.Bits);

        Assert.Equal(original.Width, restored.Width);
        Assert.Equal(original.Height, restored.Height);
        for (var i = 0; i < original.CellCount; i++)
        {
            Assert.Equal(original.IsWalkable(i), restored.IsWalkable(i));
        }
    }

    [Fact]
    public void FromBits_rejects_wrong_word_count()
    {
        Assert.Throws<ArgumentException>(() => GridMap.FromBits(13, 5, new ulong[3]));
        Assert.Throws<ArgumentException>(() => GridMap.FromBits(13, 5, new ulong[1]));
    }

    [Fact]
    public void FromBits_copies_rather_than_aliases_the_input()
    {
        var words = new ulong[2];
        words[0] = 1UL; //* cell 0 walkable

        var map = GridMap.FromBits(13, 5, words);
        words[0] = 0UL; //* mutate the source

        Assert.True(map.IsWalkable(0)); //* cell0 must be still walkable
    }
}
