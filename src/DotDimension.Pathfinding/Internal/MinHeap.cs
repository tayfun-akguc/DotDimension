namespace DotDimension.Pathfinding.Internal;

internal static class MinHeap
{
    public static void PushSwap(int[] items, float[] keys, ref int count, int item, float key)
    {
        int i = count;
        items[i] = item;
        keys[i] = key;
        count++;

        while (i > 0)
        {
            int parent = (i - 1) >> 1;
            if (keys[parent] <= keys[i])
            {
                break;
            }

            var tempKey = keys[i];
            keys[i] = keys[parent];
            keys[parent] = tempKey;

            var tempItem = items[i];
            items[i] = items[parent];
            items[parent] = tempItem;

            i = parent;
        }
    }

    //* items = cellIndex => cellIndex of the grid point => (x,y) => y * width + x
    //* keys = priority of the cellIndex => items[0] priority is placed in items[0]
    //* items and keys are both parallel arrays of the same length. items store identities, keys store priorities.
    //* instead of swapping the items, shift larger parents down one level
    //* then write the new item and key to its final position
    public static void Push(int[] items, float[] keys, ref int count, int item, float key)
    {
        var i = count++;
        while (i > 0)
        {
            var parent = (i - 1) >> 1;
            if (keys[parent] <= key)
            {
                break;
            }

            items[i] = items[parent];
            keys[i] = keys[parent];
            i = parent;
        }

        items[i] = item;
        keys[i] = key;
    }


    public static int Pop(int[] items, float[] keys, ref int count, out float key)
    {
        var topItem = items[0];
        key = keys[0];

        count--;
        var lastItem = items[count];
        var lastKey = keys[count];

        var i = 0;
        while (true)
        {
            var child = 2 * i + 1;
            if (child >= count)
            {
                break;
            }

            if (child + 1 < count && keys[child + 1] < keys[child])
            {
                child++;
            }

            if (keys[child] >= lastKey)
            {
                break;
            }

            items[i] = items[child];
            keys[i] = keys[child];
            i = child;
        }

        items[i] = lastItem;
        keys[i] = lastKey;

        return topItem;
    }
}
