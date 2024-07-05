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
    private UnsafeList<TlsfAllocation> _tlsfLocalList = new();
    private UnsafeList<nint> _libcLocalList = new();
    private Random _random = new Random();

    private static int[] AllocSizes = [64, 96, 150, 200, 400, 1024, 4096];
    
    private const int AllocationCount = 2048;

    [GlobalSetup]
    public void Setup()
    {
        _random = new Random(42);
        Console.WriteLine("Setup");
        _tlsfAllocator = new TlsfAllocator(_instance, 64);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        Console.WriteLine("Cleanup");
        _tlsfAllocator.Reset();
    }

    private uint GetNextRandomSize() => (uint)AllocSizes[_random.Next(AllocSizes.Length)];

    [Benchmark]
    public void Tlsf()
    {
        ref var localList = ref _tlsfLocalList;
        localList.Clear();

        for (int i = 0; i < AllocationCount; i++)
        {
            lock (_tlsfAllocator) // Make it more fair to the libc benchmark
            {
                var allocate = _tlsfAllocator.Allocate(GetNextRandomSize());
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

        for (int i = 0; i < AllocationCount; i++)
        {
            var allocate = NativeMemory.AlignedAlloc(GetNextRandomSize(), 64);
            localList.Add((nint)allocate);
        }

        for (int i = 0; i < AllocationCount; i++)
        {
            NativeMemory.AlignedFree((void*)localList[i]);
        }
    }

    private unsafe class ChunkAllocator : IMemoryChunkAllocator
    {
        private Dictionary<int, MemoryChunk> _chunks = new Dictionary<int, MemoryChunk>();
        private const int ChunkSize = 65536;
        
        public MemoryChunk AllocateChunk(MemorySize minSize)
        {
            var address = NativeMemory.Alloc(ChunkSize);
            var chunk = new MemoryChunk((ulong)_chunks.Count, (ulong)address, ChunkSize);
            _chunks.Add(_chunks.Count, chunk);
            return chunk;
        }

        public void FreeChunk(in MemoryChunk chunk)
        {
            NativeMemory.Free((void*)(ulong)chunk.BaseAddress);
            _chunks.Remove((int)chunk.Id.Value);
        }
    }
}