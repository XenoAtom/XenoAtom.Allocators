// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace XenoAtom.Allocators;

public readonly struct TlsfChunk
{
    private readonly TlsfAllocator.Chunk _chunk;

    /// <summary>
    /// Gets the associated chunk.
    /// </summary>
    [UnscopedRef]
    public ref readonly MemoryChunk Info => ref _chunk.Info;

    public MemorySize TotalAllocated => _chunk.TotalAllocated;

    public uint UsedBlockCount => _chunk.UsedBlockCount;

    public uint FreeBlockCount => _chunk.FreeBlockCount;
}