// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace XenoAtom.Allocators;

/// <summary>
/// Represents a memory chunk identifier.
/// </summary>
public readonly record struct MemoryChunkId(ulong Value)
{
    /// <summary>
    /// Implicit conversion from <see cref="MemoryChunkId"/> to <see cref="ulong"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ulong(MemoryChunkId value) => value.Value;

    /// <summary>
    /// Implicit conversion from <see cref="ulong"/> to <see cref="MemoryChunkId"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator MemoryChunkId(ulong value) => new(value);

    /// <inheritdoc />
    public override string ToString() => $"MemoryChunkId(0x{Value:X})";
}