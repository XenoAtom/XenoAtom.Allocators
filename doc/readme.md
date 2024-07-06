# XenoAtom.Allocators User Guide

- [Overview](#overview)
  - [Creating a TLSF allocator](#creating-a-tlsf-allocator)
  - [Allocating memory](#allocating-memory)
  - [Freeing memory](#freeing-memory)
- [Advanced usage](#advanced-usage)
  - [Customizing the alignment](#customizing-the-alignment)
  - [Custom memory chunk allocator](#custom-memory-chunk-allocator)
  - [Resetting the allocator](#resetting-the-allocator)

## Overview

The main entry point of the library is the `TlsfAllocator` class. This class provides a TLSF (Two-Level Segregated Fit) allocator implementation. The TLSF allocator is a low-level memory allocator that provides a good balance between speed, fragmentation, and memory overhead. It is particularly well suited for real-time systems.


### Creating a TLSF allocator

In order to use the TLSF allocator, you need to provide a Chunk allocator. The chunk allocator is responsible for allocating and deallocating memory chunks. The TLSF allocator will then use these chunks to allocate and deallocate memory blocks within these chunks.

You simply need to implement the interface `IMemoryChunkAllocator` that will provide the basic blocks for allocation / deallocation.

For example:

```csharp
/// <summary>
/// Implementation of a chunk allocator for the TLSF allocator using native memory.
/// </summary>
public unsafe class BasicChunkAllocator : IMemoryChunkAllocator
{
    private readonly Dictionary<int, MemoryChunk> _chunks = new Dictionary<int, MemoryChunk>();
    private const int ChunkSize = 65536;
        
    public bool TryAllocateChunk(MemorySize minSize, out MemoryChunk chunk)
    {
        var blockSize = (uint)Math.Max(ChunkSize, (int)minSize.Value);
        var address = NativeMemory.AlignedAlloc(blockSize, 64);
        chunk = new MemoryChunk((ulong)_chunks.Count, (ulong)address, blockSize);
        _chunks.Add(_chunks.Count, chunk);
        return true;
    }

    public void FreeChunk(MemoryChunkId chunkId)
    {
        var index = (int)chunkId.Value;
        var chunk = _chunks[index];
        NativeMemory.AlignedFree((void*)(ulong)chunk.BaseAddress);
        _chunks.Remove(index);
    }
}
```

Then you can create a TLSF allocator with this chunk allocator:

```csharp
var chunkAllocator = new BasicChunkAllocator();
// Create an allocator with a minimum alignment of 64 bytes
var allocator = new TlsfAllocator(chunkAllocator, 64);
```

[:top:](#xenoatomallocators-user-guide)
### Allocating memory

You can allocate memory using the `Allocate` method:

```csharp
var allocation = allocator.Allocate(128);
```

The `Allocate` method returns an `TlsfAllocation` structure that represents the allocated memory. 

A TlsfAllocation has the following properties, with Address and Size representing the address and size of the allocated block:

```csharp
public readonly record struct TlsfAllocation
{
    /// <summary>
    /// Gets the index of the block allocated (used internally by the allocator).
    /// </summary>
    public readonly TlsfAllocationToken Token;

    /// <summary>
    /// Gets the address of the allocated block.
    /// </summary>
    public readonly MemoryAddress Address;

    /// <summary>
    /// Gets the size of the allocated block.
    /// </summary>
    public readonly MemorySize Size;
}
```

[:top:](#xenoatomallocators-user-guide)
### Freeing memory

You can free memory using the `Free` method by passing a `TlsfAllocationToken` stored in the `TlsfAllocation` structure via the `Token` property:

```csharp
allocator.Free(allocation.Token);
// Or you can use the implicit cast operator:
// allocator.Free(allocation);
```

[:top:](#xenoatomallocators-user-guide)
## Advanced usage

### Customizing the alignment

You can customize the alignment of the allocator by providing a different alignment value when creating the allocator:

```csharp
var allocator = new TlsfAllocator(chunkAllocator, 128);
```

[:top:](#xenoatomallocators-user-guide)
### Custom memory chunk allocator

The chunk allocator can be customized to use different memory sources (e.g., native memory, managed memory, custom memory like GPU, etc.). 

It can also the faked with no allocations (as done in the tests) to simulate the behavior of the allocator without actually allocating memory.

[:top:](#xenoatomallocators-user-guide)
### Resetting the allocator

You can reset the allocator to its initial state by calling the `Reset` method:

```csharp
allocator.Reset();
```

It will free all the memory chunks by calling `IMemoryChunkAllocator.FreeChunk` for each allocated chunk and reset the allocator to its initial state.

[:top:](#xenoatomallocators-user-guide)
