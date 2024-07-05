// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Allocators;

/// <summary>
/// A token representing an allocation from a <see cref="TlsfAllocator"/> returned by <see cref="TlsfAllocation.Token"/>.
/// </summary>
public readonly record struct TlsfAllocationToken
{
    internal TlsfAllocationToken(uint blockIndex)
    {
        BlockIndex = blockIndex;
    }

    internal readonly uint BlockIndex;
    
    /// <inheritdoc />
    public override string ToString() => $"TlsfAllocationToken({BlockIndex})";
}