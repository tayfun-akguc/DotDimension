- an island 128 * 128 = 16384 cells
- for walkable, or not, need a bit for each cell. in c# bool = 8 bits, 7 bits wasted
- 128 x 128 = 16384 cells * 8 bits = 131072 bits
- 16384 cells * 7 bits = 114688 bits wasted
- 100000 island = 100000 * 131072 bits = 13107200000 bits = 1.6384 GB used
- 100000 island = 100000 * 114688 bits = 11468800000 bits = 1.4336 GB wasted

- use new BitArray ??

- ulong 64 bit = 1 cpu word length

# bitmask

| CellCount (map) | Exact need ((CellCount + 63) >> 6) | Rounded up (words) | Allocated bits | Wasted bits | Memory     |
|-----------------|------------------------------------|--------------------|----------------|-------------|------------|
| 1 (1×1)         | 0.015625                           | 1 (ulong1)         | 64             | 63          | 8 byte     |
| 63              | 0.984375                           | 1 (ulong1)         | 64             | 1           | 8 byte     |
| 64(8x8)         | 1                                  | 1 (ulong1)         | 64             | 0           | 8 byte     |
| 65              | 1.015625                           | 2 (ulong2)         | 128            | 63          | 16 byte    |
| 100 (10×10)     | 1.5625                             | 2 (ulong2)         | 128            | 28          | 16 byte    |
| 128             | 2                                  | 2 (ulong2)         | 128            | 0           | 16 byte    |
| 129             | 2.015625                           | 3 (ulong3)         | 192            | 63          | 24 byte    |
| 1000            | 15.625                             | 16 (ulong16)       | 1204           | 24          | 128 byte   |
| 16384(128*128)  | 256                                | 256 (ulong256)     | 16384          | 0           | 2048 byte  |
| 260100(510*510) | 4064.0625                          | 4065 (ulong4065)   | 260160         | 60          | 32520 byte |

- inBounds(x,y): bool
- toIndex(x, y): cell = y * width + x
- toPoint(): x,y
- setWalkable(x,y, isWalkable)
- setWalkable(cell, isWalkable)
- serialize / deserialize
- restoreFromBits
- restoreFromBools


- for a* use minHeap(logn)?? how to zero allocation?
- d-ary heap (4 / 8) ????
- try popFloyd for minheap??
