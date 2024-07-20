// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Allocators;

#region TlsfAllocation

/// <summary>
/// An allocation from a <see cref="TlsfAllocator"/> returned by <see cref="TlsfAllocator.Allocate"/>.
/// </summary>
public readonly record struct TlsfAllocation
{
    internal TlsfAllocation(TlsfAllocationToken token, MemoryChunkId chunkId, MemoryAddress address, MemorySize size)
    {
        Token = token;
        ChunkId = chunkId;
        Address = address;
        Size = size;
    }

    /// <summary>
    /// Gets the index of the block allocated (used internally by the allocator).
    /// </summary>
    public readonly TlsfAllocationToken Token;

    /// <summary>
    /// Gets the chunk id of this allocated block belongs to.
    /// </summary>
    public readonly MemoryChunkId ChunkId;

    /// <summary>
    /// Gets the address of the allocated block.
    /// </summary>
    public readonly MemoryAddress Address;

    /// <summary>
    /// Gets the size of the allocated block.
    /// </summary>
    public readonly MemorySize Size;

    /// <summary>
    /// Implicit conversion to <see cref="TlsfAllocationToken"/>.
    /// </summary>
    public static implicit operator TlsfAllocationToken (TlsfAllocation allocation) => allocation.Token;
}

#endregion