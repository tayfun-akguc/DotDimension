using DotDimension.Pathfinding.Internal;

namespace DotDimension.Pathfinding.Tests;

public sealed class MinHeapTests
{
    //* return item, key pairs in pop order
    private static List<(int Item, float Key)> Drain(int[] items, float[] keys, ref int count)
    {
        var result = new List<(int, float)>();
        while (count > 0)
        {
            var item = MinHeap.Pop(items, keys, ref count, out var key);
            result.Add((item, key));
        }

        return result;
    }

    [Fact]
    public void Push_into_empty_heap_places_item_at_root()
    {
        var items = new int[4];
        var keys = new float[4];
        var count = 0;

        MinHeap.Push(items, keys, ref count, item: 42, key: 3.5f);

        Assert.Equal(1, count);
        Assert.Equal(42, items[0]);
        Assert.Equal(3.5f, keys[0]);
    }

    [Fact]
    public void Pop_single_element_empties_the_heap()
    {
        var items = new int[4];
        var keys = new float[4];
        var count = 0;
        MinHeap.Push(items, keys, ref count, 42, 3.5f);

        var popped = MinHeap.Pop(items, keys, ref count, out var key);

        Assert.Equal(42, popped);
        Assert.Equal(3.5f, key);
        Assert.Equal(0, count);
    }

    [Fact]
    public void Second_push_with_smaller_key_becomes_the_new_root()
    {
        var items = new int[4];
        var keys = new float[4];
        var count = 0;

        MinHeap.Push(items, keys, ref count, 1, 10f);
        MinHeap.Push(items, keys, ref count, 2, 5f);

        Assert.Equal(2, items[0]);
        Assert.Equal(5f, keys[0]);
    }

    [Fact]
    public void Pops_come_out_in_ascending_key_order()
    {
        var items = new int[16];
        var keys = new float[16];
        var count = 0;

        float[] insertKeys = { 7f, 1f, 9f, 3f, 8f, 2f, 6f, 4f, 5f };
        for (var i = 0; i < insertKeys.Length; i++)
        {
            MinHeap.Push(items, keys, ref count, item: i, key: insertKeys[i]);
        }

        var drained = Drain(items, keys, ref count);

        var poppedKeys = drained.Select(e => e.Key).ToArray();
        var sorted = insertKeys.OrderBy(k => k).ToArray();
        Assert.Equal(sorted, poppedKeys);
    }

    [Fact]
    public void Items_stay_paired_with_their_own_keys()
    {
        var items = new int[16];
        var keys = new float[16];
        var count = 0;

        int[] insertOrder = { 5, 0, 7, 2, 6, 1, 4, 3 };
        foreach (var i in insertOrder)
        {
            MinHeap.Push(items, keys, ref count, item: i, key: i * 10f);
        }

        var drained = Drain(items, keys, ref count);

        Assert.Equal(insertOrder.Length, drained.Count);
        foreach (var (item, key) in drained)
        {
            Assert.Equal(item * 10f, key);
        }
    }

    [Fact]
    public void Duplicate_keys_are_all_returned()
    {
        var items = new int[8];
        var keys = new float[8];
        var count = 0;

        MinHeap.Push(items, keys, ref count, 10, 5f);
        MinHeap.Push(items, keys, ref count, 20, 5f);
        MinHeap.Push(items, keys, ref count, 30, 1f);
        MinHeap.Push(items, keys, ref count, 40, 5f);

        var drained = Drain(items, keys, ref count);

        Assert.Equal(30, drained[0].Item);
        var tieItems = drained.Skip(1).Select(e => e.Item).OrderBy(i => i);
        Assert.Equal(new[] { 10, 20, 40 }, tieItems);
        Assert.All(drained.Skip(1), e => Assert.Equal(5f, e.Key));
    }

    [Fact]
    public void Mock_test()
    {
        const int n = 10;
        var items = new int[n];
        var keys = new float[n];
        var count = 0;

        MinHeap.Push(items, keys, ref count, 10, 10);
        MinHeap.Push(items, keys, ref count, 9, 9);
        MinHeap.Push(items, keys, ref count, 8, 8);

        Assert.NotEmpty(items);
        Assert.Equal(8, items[0]);
        Assert.Equal(10, items[1]);
        Assert.Equal(9, items[2]);
    }
}
