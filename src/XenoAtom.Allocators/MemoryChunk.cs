// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Allocators;

/// <summary>
/// Represents a memory chunk allocated from an <see cref="IMemoryChunkAllocator"/>.
/// </summary>
/// <param name="id">The chunk identifier.</param>
/// <param name="baseAddress">The base address of the chunk.</param>
/// <param name="size">The size of this chunk.</param>
public struct MemoryChunk(MemoryChunkId id, MemoryAddress baseAddress, MemorySize size)
{
    /// <summary>
    /// The chunk identifier.
    /// </summary>
    public readonly MemoryChunkId Id = id;

    /// <summary>
    /// The base address of the chunk.
    /// </summary>
    public readonly MemoryAddress BaseAddress = baseAddress;

    /// <summary>
    /// The size of this chunk.
    /// </summary>
    public readonly MemorySize Size = size;
}