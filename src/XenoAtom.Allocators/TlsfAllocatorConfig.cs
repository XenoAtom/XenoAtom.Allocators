// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Allocators;

/// <summary>
/// Default configuration for <see cref="TlsfAllocator"/>.
/// </summary>
public readonly record struct TlsfAllocatorConfig()
{
    /// <summary>
    /// Gets or sets the minimum size/alignment of an allocation.
    /// </summary>
    public uint Alignment { get; init; } = 64;

    /// <summary>
    /// Gets or sets the number of chunks that we might allocate.
    /// </summary>
    public uint PreAllocatedChunkCount { get; init; } = 4;

    /// <summary>
    /// Gets or sets the number of blocks that we might allocate.
    /// </summary>
    public uint PreAllocatedBlockCount { get; init; } = 16;

    /// <summary>
    /// Gets or sets the number of available blocks that we might allocate.
    /// </summary>
    public uint PreAllocatedAvailableBlockCount { get; init; } = 16;
}