// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace XenoAtom.Allocators;

internal static class AlignHelper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong AlignUp(ulong value, uint alignment)
    {
        Debug.Assert(BitOperations.IsPow2(alignment));
        return (value + alignment - 1) & ~(ulong)(alignment - 1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint AlignUp(uint value, uint alignment)
    {
        Debug.Assert(BitOperations.IsPow2(alignment));
        return (value + alignment - 1) & ~(uint)(alignment - 1);
    }

    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint AlignUpOffset(ulong value, uint alignment)
    {
        Debug.Assert(BitOperations.IsPow2(alignment));
        return (uint)(AlignUp(value, alignment) - value);
    }
}