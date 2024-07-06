// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Allocators;

/// <summary>
/// Base interface for a memory allocator of chunks.
/// </summary>
public interface IMemoryChunkAllocator
{
    /// <summary>
    /// Allocates a new chunk of memory.
    /// </summary>
    /// <param name="minSize">The minimum size. Usually the size returned by an allocator can be much bigger.</param>
    /// <param name="chunk">The allocated memory chunk.</param>
    /// <returns><c>true</c> if the allocation was successful; otherwise <c>false</c>.</returns>
    bool TryAllocateChunk(MemorySize minSize, out MemoryChunk chunk);

    /// <summary>
    /// Frees a chunk of memory.
    /// </summary>
    /// <param name="chunk">The original chunk that was allocated.</param>
    void FreeChunk(MemoryChunkId chunk);
}