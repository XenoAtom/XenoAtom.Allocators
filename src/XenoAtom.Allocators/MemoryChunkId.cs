// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace XenoAtom.Allocators;

public readonly record struct MemoryChunkId(ulong Value)
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ulong(MemoryChunkId value) => value.Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator MemoryChunkId(ulong value) => new(value);

    public override string ToString() => $"MemoryChunkId(0x{Value:X})";
}