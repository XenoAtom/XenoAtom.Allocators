// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace XenoAtom.Allocators;

/// <summary>
/// Represents a memory size with a 32 bits unsigned integer (maximum 4GB).
/// </summary>
public readonly record struct MemorySize(uint Value)
{
    /// <summary>
    /// Implicit conversion from <see cref="MemorySize"/> to <see cref="uint"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator uint(MemorySize value) => value.Value;

    /// <summary>
    /// Implicit conversion from <see cref="uint"/> to <see cref="MemorySize"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator MemorySize(uint value) => new(value);

    /// <summary>
    /// Add a memory size by an offset.
    /// </summary>
    public static MemorySize operator +(MemorySize size, uint offset) => new(size.Value + offset);

    /// <summary>
    /// Multiplies a memory size by a value.
    /// </summary>
    public static MemorySize operator *(MemorySize size, uint value) => new(size.Value * value);

    /// <inheritdoc />
    public override string ToString() => $"MemorySize({Value})";
}