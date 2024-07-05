// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using XenoAtom.Collections;

namespace XenoAtom.Allocators.Bench;

public class BenchAllocator
{
    private static readonly ChunkAllocator _instance = new ChunkAllocator();

    private TlsfAllocator _tlsfAllocator;
    private UnsafeList<TlsfAllocation>.N64 _tlsfLocalList = new();
    private UnsafeList<nint>.N64 _libcLocalList = new();

    [GlobalSetup]
    public void Setup()
    {
        Console.WriteLine("Setup");
        _tlsfAllocator = new TlsfAllocator(_instance, 64);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        Console.WriteLine("Cleanup");
        _tlsfAllocator.Reset();
    }

    [Benchmark]
    public void Tlsf()
    {
        ref var localList = ref _tlsfLocalList;
        localList.Clear();

        for (int i = 0; i < 64; i++)
        {
            lock (_tlsfAllocator) // Make it more fair to the libc benchmark
            {
                var allocate = _tlsfAllocator.Allocate(64);
                localList.Add(allocate);
            }
        }

        for(int i = 0; i < localList.Count; i++)
        {
            lock (_tlsfAllocator) // Make it more fair to the libc benchmark
            {
                _tlsfAllocator.Free(localList[i]);
            }
        }
    }

    [Benchmark]
    public unsafe void Libc()
    {
        ref var localList = ref _libcLocalList;
        localList.Clear();

        for (int i = 0; i < 64; i++)
        {
            var allocate = NativeMemory.Alloc(64);
            localList.Add((nint)allocate);
        }

        for (int i = 0; i < 64; i++)
        {
            NativeMemory.Free((void*)localList[i]);
        }
    }

    private unsafe class ChunkAllocator : IMemoryChunkAllocator
    {
        private List<MemoryChunk> _chunks = new List<MemoryChunk>();
        private const int ChunkSize = 65536;
        
        public MemoryChunk AllocateChunk(MemorySize minSize)
        {
            Console.WriteLine("Allocate 64KB");
            var address = NativeMemory.Alloc(ChunkSize);
            var chunk = new MemoryChunk((ulong)_chunks.Count, (ulong)address, ChunkSize);
            _chunks.Add(chunk);
            return chunk;
        }

        public void FreeChunk(in MemoryChunk chunk)
        {
            NativeMemory.Free((void*)(ulong)chunk.BaseAddress);
            _chunks.RemoveAt((int)chunk.Id.Value);
        }
    }
}