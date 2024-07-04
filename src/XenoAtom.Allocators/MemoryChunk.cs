// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Allocators;

public struct MemoryChunk(MemoryChunkId id, MemoryAddress baseAddress, MemorySize size)
{
    public readonly MemoryChunkId Id = id;
    public readonly MemoryAddress BaseAddress = baseAddress;
    public readonly MemorySize Size = size;
}