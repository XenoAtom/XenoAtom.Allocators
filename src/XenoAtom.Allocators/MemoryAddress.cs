// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace XenoAtom.Allocators;

/// <summary>
/// Represents a memory address. The address is always 64 bits to support any architecture.
/// </summary>
public readonly record struct MemoryAddress(ulong Value)
{
    /// <summary>
    /// Implicit conversion from <see cref="MemoryAddress"/> to <see cref="ulong"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ulong(MemoryAddress value) => value.Value;

    /// <summary>
    /// Implicit conversion from <see cref="ulong"/> to <see cref="MemoryAddress"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator MemoryAddress(ulong value) => new(value);

    /// <summary>
    /// Adds an offset to the memory address.
    /// </summary>
    public static MemoryAddress operator +(MemoryAddress address, uint offset) => new(address.Value + offset);

    /// <inheritdoc />
    public override string ToString() => $"MemoryAddress(0x{Value:X16})";
}