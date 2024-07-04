// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace XenoAtom.Allocators;

public readonly record struct MemoryAddress(ulong Value)
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ulong(MemoryAddress value) => value.Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator MemoryAddress(ulong value) => new(value);

    public static MemoryAddress operator +(MemoryAddress address, uint offset) => new(address.Value + offset);

    public override string ToString() => $"MemoryAddress(0x{Value:X16})";
}

public readonly record struct MemorySize(uint Value)
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator uint(MemorySize value) => value.Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator MemorySize(uint value) => new(value);

    public static MemorySize operator +(MemorySize size, uint offset) => new(size.Value + offset);

    public static MemorySize operator *(MemorySize size, uint offset) => new(size.Value * offset);

    public override string ToString() => $"MemorySize({Value})";
}