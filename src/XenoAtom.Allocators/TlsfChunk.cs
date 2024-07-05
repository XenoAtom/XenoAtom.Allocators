// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace XenoAtom.Allocators;

/// <summary>
/// Represents a chunk allocated from a <see cref="TlsfAllocator"/>.
/// </summary>
public readonly struct TlsfChunk
{
    private readonly TlsfAllocator.Chunk _chunk;

    /// <summary>
    /// Gets the associated chunk.
    /// </summary>
    [UnscopedRef]
    public ref readonly MemoryChunk Info => ref _chunk.Info;

    /// <summary>
    /// Gets the total allocated size of this chunk.
    /// </summary>
    public MemorySize TotalAllocated => _chunk.TotalAllocated;

    /// <summary>
    /// Gets the number of blocks used.
    /// </summary>
    public uint UsedBlockCount => (uint)_chunk.UsedBlockCount;

    /// <summary>
    /// Gets the number of free blocks.
    /// </summary>
    public uint FreeBlockCount => (uint)_chunk.FreeBlockCount;
}