// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Allocators;

public readonly record struct TlsfAllocation(uint BlockIndex, MemoryAddress Address, MemorySize Size);