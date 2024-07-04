using System.Globalization;
using System.Text;

namespace XenoAtom.Allocators.Tests;

[TestClass]
[UsesVerify]
public partial class BasicTests
{
    public static MemoryAddress BaseAddress => 0xFE00_1200_0000_0000;

    public static MemorySize BaseChunkSize => 65536;

    [TestMethod]
    public async Task TestAllocate1()
    {
        var allocator = new TlsfChunkAllocatorTestInstance(BaseAddress, BaseChunkSize);

        MemorySize alignment = 64;
        var tlsf = new TlsfAllocator(allocator, alignment);

        Assert.AreEqual(0, tlsf.Chunks.Length);
        
        var allocation = tlsf.Allocate(512);

        // General checks
        Assert.AreEqual(1, allocator.RequestedChunkAllocations.Count);
        Assert.AreEqual(1, tlsf.Chunks.Length);
        Assert.AreEqual((MemoryChunkId)0, tlsf.Chunks[0].Info.Id);
        Assert.AreEqual(BaseAddress, tlsf.Chunks[0].Info.BaseAddress);
        Assert.AreEqual(BaseChunkSize, tlsf.Chunks[0].Info.Size);
        Assert.AreEqual(1U, tlsf.Chunks[0].UsedBlockCount);
        Assert.AreEqual(1U, tlsf.Chunks[0].FreeBlockCount);

        // Allocation 1
        Assert.AreEqual(BaseAddress, allocation.Address);
        Assert.AreEqual((MemorySize)512, allocation.Size);

        // Dump intermediate result
        await VerifyStep(tlsf, "01-Allocate1");

        // Free Allocation 1
        tlsf.Free(allocation);

        // Dump intermediate result
        await VerifyStep(tlsf, "02-Free Allocate1");

        Assert.AreEqual(0U, tlsf.Chunks[0].UsedBlockCount);
        Assert.AreEqual(1U, tlsf.Chunks[0].FreeBlockCount);

        // Resets the allocator (free chunks)
        tlsf.Reset();
        Assert.AreEqual(0, tlsf.Chunks.Length);
        Assert.AreEqual(0, allocator.RequestedChunkAllocations.Count);
    }

    [TestMethod]
    public async Task TestAllocate3()
    {
        var allocator = new TlsfChunkAllocatorTestInstance(BaseAddress, BaseChunkSize);

        MemorySize alignment = 1024;
        var tlsf = new TlsfAllocator(allocator, alignment);

        Assert.AreEqual(0, tlsf.Chunks.Length);

        var allocation1 = tlsf.Allocate(512);
        var allocation2 = tlsf.Allocate(alignment);
        var allocation3 = tlsf.Allocate(1025);

        // General checks
        Assert.AreEqual(1, allocator.RequestedChunkAllocations.Count);
        Assert.AreEqual(1, tlsf.Chunks.Length);
        Assert.AreEqual((MemoryChunkId)0, tlsf.Chunks[0].Info.Id);
        Assert.AreEqual(BaseAddress, tlsf.Chunks[0].Info.BaseAddress);
        Assert.AreEqual(BaseChunkSize, tlsf.Chunks[0].Info.Size);
        Assert.AreEqual(3U, tlsf.Chunks[0].UsedBlockCount);
        Assert.AreEqual(1U, tlsf.Chunks[0].FreeBlockCount);
        Assert.AreEqual(allocation1.Size + allocation2.Size + allocation3.Size, tlsf.Chunks[0].TotalAllocated);

        // Allocation 1
        Assert.AreEqual(BaseAddress, allocation1.Address);
        Assert.AreEqual(alignment, allocation1.Size);

        // Allocation 2
        Assert.AreEqual(BaseAddress + alignment, allocation2.Address);
        Assert.AreEqual(alignment, allocation2.Size);

        // Allocation 3
        Assert.AreEqual(BaseAddress + alignment + alignment, allocation3.Address);
        Assert.AreEqual(alignment * 2, allocation3.Size);

        // Dump intermediate result
        await VerifyStep(tlsf, "01-Allocations");

        // Free allocation 1
        tlsf.Free(allocation1);
        Assert.AreEqual(2U, tlsf.Chunks[0].UsedBlockCount);
        Assert.AreEqual(2U, tlsf.Chunks[0].FreeBlockCount);

        // Free allocation 3
        tlsf.Free(allocation3);
        Assert.AreEqual(1U, tlsf.Chunks[0].UsedBlockCount);
        Assert.AreEqual(2U, tlsf.Chunks[0].FreeBlockCount);

        // Free allocation 2
        tlsf.Free(allocation2);
        Assert.AreEqual(0U, tlsf.Chunks[0].UsedBlockCount);
        Assert.AreEqual(1U, tlsf.Chunks[0].FreeBlockCount);

        await VerifyStep(tlsf, "02-Free Allocations");
        
        // Resets the allocator (free chunks)
        tlsf.Reset();
        Assert.AreEqual(0, tlsf.Chunks.Length);
        Assert.AreEqual(0, allocator.RequestedChunkAllocations.Count);
    }

    [TestMethod]
    public async Task TestAllocateBiggerThanChunk()
    {
        var allocator = new TlsfChunkAllocatorTestInstance(BaseAddress, BaseChunkSize);

        MemorySize alignment = 64;
        var tlsf = new TlsfAllocator(allocator, alignment);

        Assert.AreEqual(0, tlsf.Chunks.Length);

        var allocation = tlsf.Allocate(BaseChunkSize + 5);

        // General checks
        Assert.AreEqual(1, allocator.RequestedChunkAllocations.Count);
        Assert.AreEqual(1, tlsf.Chunks.Length);
        Assert.AreEqual((MemoryChunkId)0, tlsf.Chunks[0].Info.Id);
        Assert.AreEqual(BaseAddress, tlsf.Chunks[0].Info.BaseAddress);
        Assert.AreEqual(BaseChunkSize * 2, tlsf.Chunks[0].Info.Size);
        Assert.AreEqual(1U, tlsf.Chunks[0].UsedBlockCount);
        Assert.AreEqual(1U, tlsf.Chunks[0].FreeBlockCount);

        // Allocation 1
        Assert.AreEqual(BaseAddress, allocation.Address);
        Assert.AreEqual((MemorySize)(BaseChunkSize + alignment), allocation.Size);

        await VerifyStep(tlsf, "01-Allocation");
        
        // Free Allocation 1
        tlsf.Free(allocation);
        Assert.AreEqual(0U, tlsf.Chunks[0].UsedBlockCount);
        Assert.AreEqual(1U, tlsf.Chunks[0].FreeBlockCount);

        await VerifyStep(tlsf, "02-Free Allocation");

        // Resets the allocator (free chunks)
        tlsf.Reset();
        Assert.AreEqual(0, tlsf.Chunks.Length);
        Assert.AreEqual(0, allocator.RequestedChunkAllocations.Count);
    }


    [TestMethod]
    public async Task TestAllocate2Chunks()
    {
        var allocator = new TlsfChunkAllocatorTestInstance(BaseAddress, BaseChunkSize);

        MemorySize alignment = 64;
        var tlsf = new TlsfAllocator(allocator, alignment);

        Assert.AreEqual(0, tlsf.Chunks.Length);

        var allocation1 = tlsf.Allocate(4096);

        // Dump intermediate result
        await VerifyStep(tlsf, "01-Allocation1");

        // We want to force the allocation of a new chunk, despite the fact that we have a first level bin that will report it free
        // but not the second level that won't be enough big to accomodate the requested size
        var allocation2 = tlsf.Allocate(BaseChunkSize - 65);

        // Dump intermediate result
        await VerifyStep(tlsf, "02-Allocation2");

        // General checks
        Assert.AreEqual(2, allocator.RequestedChunkAllocations.Count);
        Assert.AreEqual(2, tlsf.Chunks.Length);
        Assert.AreEqual((MemoryChunkId)0, tlsf.Chunks[0].Info.Id);
        Assert.AreEqual((MemoryChunkId)1, tlsf.Chunks[1].Info.Id);
        Assert.AreEqual(BaseAddress, tlsf.Chunks[0].Info.BaseAddress);
        Assert.AreEqual(BaseAddress + BaseChunkSize, tlsf.Chunks[1].Info.BaseAddress);
        Assert.AreEqual(BaseChunkSize, tlsf.Chunks[0].Info.Size);
        Assert.AreEqual(BaseChunkSize, tlsf.Chunks[1].Info.Size);
        Assert.AreEqual(1U, tlsf.Chunks[0].UsedBlockCount);
        Assert.AreEqual(1U, tlsf.Chunks[0].FreeBlockCount);
        Assert.AreEqual(1U, tlsf.Chunks[1].UsedBlockCount);
        Assert.AreEqual(1U, tlsf.Chunks[1].FreeBlockCount);

        // Allocation 1
        Assert.AreEqual(BaseAddress, allocation1.Address);
        Assert.AreEqual((MemorySize)4096, allocation1.Size);


        // Allocation 2
        Assert.AreEqual(BaseAddress + BaseChunkSize, allocation2.Address);
        Assert.AreEqual((MemorySize)(BaseChunkSize - 64), allocation2.Size);

        // Free Allocation 1
        tlsf.Free(allocation1);

        // Dump intermediate result
        await VerifyStep(tlsf, "03-Free Allocation1");

        // Free Allocation 2
        tlsf.Free(allocation2);

        // Dump intermediate result
        await VerifyStep(tlsf, "04-Free Allocation2");
        
        Assert.AreEqual(0U, tlsf.Chunks[0].UsedBlockCount);
        Assert.AreEqual(1U, tlsf.Chunks[0].FreeBlockCount);
        Assert.AreEqual(0U, tlsf.Chunks[1].UsedBlockCount);
        Assert.AreEqual(1U, tlsf.Chunks[1].FreeBlockCount);

        // Resets the allocator (free chunks)
        tlsf.Reset();
        Assert.AreEqual(0, tlsf.Chunks.Length);
        Assert.AreEqual(0, allocator.RequestedChunkAllocations.Count);
    }

    [TestMethod]
    public async Task TestAllocate2Chunks1()
    {
        var allocator = new TlsfChunkAllocatorTestInstance(BaseAddress, BaseChunkSize);

        MemorySize alignment = 64;
        var tlsf = new TlsfAllocator(allocator, alignment);

        Assert.AreEqual(0, tlsf.Chunks.Length);

        var allocation1 = tlsf.Allocate(960); // Allocate 

        // Dump intermediate result
        await VerifyStep(tlsf, "01-Allocation1");

        // We want to force the allocation of a new chunk, despite the fact that we have a first level bin that will report it free
        // but not the second level that won't be enough big to accomodate the requested size
        var allocation2 = tlsf.Allocate(BaseChunkSize - 65);

        // Dump intermediate result
        await VerifyStep(tlsf, "02-Allocation2");

        // General checks
        Assert.AreEqual(2, allocator.RequestedChunkAllocations.Count);
        Assert.AreEqual(2, tlsf.Chunks.Length);
        Assert.AreEqual((MemoryChunkId)0, tlsf.Chunks[0].Info.Id);
        Assert.AreEqual((MemoryChunkId)1, tlsf.Chunks[1].Info.Id);
        Assert.AreEqual(BaseAddress, tlsf.Chunks[0].Info.BaseAddress);
        Assert.AreEqual(BaseAddress + BaseChunkSize, tlsf.Chunks[1].Info.BaseAddress);
        Assert.AreEqual(BaseChunkSize, tlsf.Chunks[0].Info.Size);
        Assert.AreEqual(BaseChunkSize, tlsf.Chunks[1].Info.Size);
        Assert.AreEqual(1U, tlsf.Chunks[0].UsedBlockCount);
        Assert.AreEqual(1U, tlsf.Chunks[0].FreeBlockCount);
        Assert.AreEqual(1U, tlsf.Chunks[1].UsedBlockCount);
        Assert.AreEqual(1U, tlsf.Chunks[1].FreeBlockCount);

        // Allocation 1
        Assert.AreEqual(BaseAddress, allocation1.Address);
        Assert.AreEqual((MemorySize)960, allocation1.Size);


        // Allocation 2
        Assert.AreEqual(BaseAddress + BaseChunkSize, allocation2.Address);
        Assert.AreEqual((MemorySize)(BaseChunkSize - 64), allocation2.Size);

        // Free Allocation 1
        tlsf.Free(allocation1);

        // Dump intermediate result
        await VerifyStep(tlsf, "03-Free Allocation1");

        // Free Allocation 2
        tlsf.Free(allocation2);

        // Dump intermediate result
        await VerifyStep(tlsf, "04-Free Allocation2");

        Assert.AreEqual(0U, tlsf.Chunks[0].UsedBlockCount);
        Assert.AreEqual(1U, tlsf.Chunks[0].FreeBlockCount);
        Assert.AreEqual(0U, tlsf.Chunks[1].UsedBlockCount);
        Assert.AreEqual(1U, tlsf.Chunks[1].FreeBlockCount);

        // Resets the allocator (free chunks)
        tlsf.Reset();
        Assert.AreEqual(0, tlsf.Chunks.Length);
        Assert.AreEqual(0, allocator.RequestedChunkAllocations.Count);
    }
    
    [TestMethod]
    public async Task TestAllocateExactlyOneChunk()
    {
        var allocator = new TlsfChunkAllocatorTestInstance(BaseAddress, BaseChunkSize);

        MemorySize alignment = 64;
        var tlsf = new TlsfAllocator(allocator, alignment);

        Assert.AreEqual(0, tlsf.Chunks.Length);

        var allocation = tlsf.Allocate(BaseChunkSize);

        await VerifyStep(tlsf, "01-Allocation");

        // General checks
        Assert.AreEqual(1, allocator.RequestedChunkAllocations.Count);
        Assert.AreEqual(1, tlsf.Chunks.Length);
        Assert.AreEqual((MemoryChunkId)0, tlsf.Chunks[0].Info.Id);
        Assert.AreEqual(BaseAddress, tlsf.Chunks[0].Info.BaseAddress);
        Assert.AreEqual(BaseChunkSize, tlsf.Chunks[0].Info.Size);
        Assert.AreEqual(1U, tlsf.Chunks[0].UsedBlockCount);
        Assert.AreEqual(0U, tlsf.Chunks[0].FreeBlockCount);

        // Allocation 1
        Assert.AreEqual(BaseAddress, allocation.Address);
        Assert.AreEqual(BaseChunkSize , allocation.Size);

        // Free Allocation 1
        tlsf.Free(allocation);
        Assert.AreEqual(0U, tlsf.Chunks[0].UsedBlockCount);
        Assert.AreEqual(1U, tlsf.Chunks[0].FreeBlockCount);

        await VerifyStep(tlsf, "01-Free Allocation");

        // Resets the allocator (free chunks)
        tlsf.Reset();
        Assert.AreEqual(0, tlsf.Chunks.Length);
        Assert.AreEqual(0, allocator.RequestedChunkAllocations.Count);
    }
    
    private async Task VerifyStep(TlsfAllocator tlsf, string stepName)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        var builder = new StringBuilder();

        var settings = new VerifySettings();
        settings.UseDirectory("Verified");
        settings.DisableDiff();

        tlsf.Dump(builder);
        settings.UseMethodName($"{TestContext!.FullyQualifiedTestClassName}.{TestContext.ManagedMethod}.Case_{stepName.Replace(" ", "_")}");
        
        var text = builder.ToString().ReplaceLineEndings("\n");

        await Verify(text, settings);
    }
    
    private record struct ChunkAllocationRequest(MemoryChunkId Id, MemoryAddress BaseAddress, MemorySize RequestedSize, MemorySize AllocatedSize);

    private class TlsfChunkAllocatorTestInstance : IMemoryChunkAllocator
    {
        private readonly uint _baseSize;
        private uint _nextSize;
        private ulong _baseAddress;

        public TlsfChunkAllocatorTestInstance(ulong baseAddress, uint baseSize)
        {
            RequestedChunkAllocations = new List<ChunkAllocationRequest>();
            _baseAddress = baseAddress;
            _baseSize = baseSize;
            _nextSize = baseSize;
        }

        public void Reset()
        {
            RequestedChunkAllocations.Clear();
            _nextSize = _baseSize;
        }
        
        public List<ChunkAllocationRequest> RequestedChunkAllocations { get; }
        
        public MemoryChunk AllocateChunk(MemorySize size)
        {
            while (_nextSize < size)
            {
                _nextSize *= 2;
            }

            var chunkIndex = RequestedChunkAllocations.Count;
            var baseAddress = _baseAddress;
            _baseAddress += _nextSize;
            RequestedChunkAllocations.Add(new((ulong)chunkIndex, baseAddress, size, _nextSize));

            return new((ulong)chunkIndex, baseAddress, _nextSize);
        }

        public void FreeChunk(in MemoryChunk chunk)
        {
            for (var i = 0; i < RequestedChunkAllocations.Count; i++)
            {
                if (RequestedChunkAllocations[i].Id == chunk.Id)
                {
                    RequestedChunkAllocations.RemoveAt(i);
                    return;
                }
            }
        }
    }
}