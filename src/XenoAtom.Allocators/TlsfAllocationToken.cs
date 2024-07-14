// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;

namespace XenoAtom.Allocators;

/// <summary>
/// A token representing an allocation from a <see cref="TlsfAllocator"/> returned by <see cref="TlsfAllocation.Token"/>.
/// </summary>
public readonly struct TlsfAllocationToken : IEquatable<TlsfAllocationToken>
{
    private readonly int _blockIndex;

    internal TlsfAllocationToken(int blockIndex)
    {
        // Block index is stored with the operator ~ to be able to detect if the token is valid or not.
        _blockIndex = ~blockIndex;
    }

    /// <summary>
    /// Returns true if this token is valid.
    /// </summary>
    public bool IsValid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _blockIndex < 0;
    }

    internal int BlockIndex => ~_blockIndex;
    
    /// <inheritdoc />
    public override string ToString() => $"TlsfAllocationToken({BlockIndex})";

    public bool Equals(TlsfAllocationToken other)
    {
        return _blockIndex == other._blockIndex;
    }

    public override bool Equals(object? obj)
    {
        return obj is TlsfAllocationToken other && Equals(other);
    }

    public override int GetHashCode()
    {
        return _blockIndex;
    }

    public static bool operator ==(TlsfAllocationToken left, TlsfAllocationToken right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(TlsfAllocationToken left, TlsfAllocationToken right)
    {
        return !left.Equals(right);
    }
}