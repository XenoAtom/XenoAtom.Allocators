// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using XenoAtom.Collections;

namespace XenoAtom.Allocators;

/// <summary>
/// This is a TLSF (Two-Level Segregated Fit) allocator following the paper http://www.gii.upv.es/tlsf/files/papers/ecrts04_tlsf.pdf
/// 
/// But with the following modifications:
/// - We are relying on a backend allocator for the chunks.
/// - We are not storing the block headers in the allocated memory but in separate array as the memory allocated from chunks might not be accessible from CPU (e.g GPU).
/// 
/// With its backend allocator, this allocator is dynamic and its size can grow as needed. This allocator doesn't allocate memory by itself,
/// but use a backend allocator to allocate chunks of memory. It is agnostic of the backend allocator (that can allocate memory from RAM or GPU memory...etc.).
/// </summary>
/// <remarks>
/// The minimum alignment is 64 bytes. The memory overhead per allocation request is 32 bytes.
/// Note that this class is not thread safe and should be guarded by a lock if used in a multi-threaded environment.
/// The rationale is that this allocator can be used with Thread Local Storage (TLS) buffers that are not shared between threads and don't need locking.
/// </remarks>
public sealed unsafe class TlsfAllocator
{
    private readonly IMemoryChunkAllocator _context;
    private readonly uint _alignment;
    private UnsafeList<Chunk> _chunks;
    private UnsafeList<Block> _blocks;
    private int _indexToFirstAvailableBlock;
    private BinsDirectory _bins;

    // Constants
    private const int BaseBin0Log2 = 10; // Maximum 1024 bytes in the first level bin
    private const int BinCount = 32 - BaseBin0Log2;
    private const int SubBinsLog2 = 4; // It means that we have 16 sub-bins - so an ushort per bin for the bitmap
    private const int SubBinCount = 1 << SubBinsLog2;
    private const int TotalBinCount = BinCount * SubBinCount;
    private const int MinAlignment = 1 << (BaseBin0Log2 - SubBinsLog2); // Minimum alignment is 64 bytes

    /// <summary>
    /// Creates a new instance of <see cref="TlsfAllocator"/>.
    /// </summary>
    /// <param name="context">The allocator context.</param>
    /// <param name="alignment">The requested alignment.</param>
    public TlsfAllocator(IMemoryChunkAllocator context, uint alignment) : this(context, new TlsfAllocatorConfig() { Alignment = alignment })
    {
    }

    /// <summary>
    /// Creates a new instance of <see cref="TlsfAllocator"/>.
    /// </summary>
    /// <param name="context">The allocator context.</param>
    /// <param name="config">The configuration of this allocator.</param>
    /// <exception cref="InvalidOperationException">If the alignment is not a power of 2.</exception>
    public TlsfAllocator(IMemoryChunkAllocator context, in TlsfAllocatorConfig config)
    {
        Debug.Assert(sizeof(Block) == 32);

        var alignment = config.Alignment;
        if (!BitOperations.IsPow2(alignment))
        {
            throw new InvalidOperationException($"Alignment must be a power of 2. Alignment value `{alignment}` is invalid.");
        }
        _context = context;
        _alignment = Math.Max(MinAlignment, alignment);
        _chunks = new UnsafeList<Chunk>((int)config.PreAllocatedChunkCount);
        _blocks = new UnsafeList<Block>((int)config.PreAllocatedBlockCount);
        _indexToFirstAvailableBlock = -1;
        _bins = new BinsDirectory();
    }

    /// <summary>
    /// Gets a read-only span of the chunks allocated by this allocator.
    /// </summary>
    public ReadOnlySpan<TlsfChunk> Chunks => MemoryMarshal.Cast<Chunk, TlsfChunk>(_chunks.AsSpan());

    /// <summary>
    /// Allocate a block of memory of the specified size.
    /// </summary>
    /// <param name="size">The requested size</param>
    /// <returns>An allocation with the requested size rounded up to the alignment of this allocator.</returns>
    public TlsfAllocation Allocate(uint size)
    {
        // We align the size to the alignment (so free blocks are always aligned)
        size = AlignHelper.AlignUp(size, _alignment);

        var firstLevelIndex = Mapping(size, out int secondLevelIndex);

        ref var freeBlock = ref FindSuitableBlock(size, ref firstLevelIndex, ref secondLevelIndex, out var freeBlockIndex);

        ref var chunk = ref _chunks.UnsafeGetRefAt((int)freeBlock.ChunkIndex);
        chunk.TotalAllocated += size;

        var offsetIntoChunk = freeBlock.OffsetIntoChunk;
        var newFreeBlockSize = freeBlock.Size - size;
        
        if (newFreeBlockSize > 0)
        {
            // we need to shrink the block and create a new block used
            freeBlock.OffsetIntoChunk = offsetIntoChunk + size;
            freeBlock.Size = newFreeBlockSize;

            // Create a new block
            var usedBlockIndex = GetNextAvailableBlockIndex();
            ref var usedBlock = ref GetOrCreateBlockAt(usedBlockIndex);
            chunk.UsedBlockCount++;
            // We need to rebind the free block as we might have allocate a new block with UnsafeGetOrCreate
            freeBlock = ref GetBlockAt(freeBlockIndex);

            usedBlock.ChunkIndex = freeBlock.ChunkIndex;
            usedBlock.OffsetIntoChunk = offsetIntoChunk;
            usedBlock.Size = size;
            usedBlock.IsUsed = true;
            usedBlock.FreeLink = BlockLinks.Undefined;
            
            // Insert the new block in the physical order
            usedBlock.PhysicalLink.Next = freeBlockIndex;
            usedBlock.PhysicalLink.Previous = freeBlock.PhysicalLink.Previous;
            
            if (freeBlock.PhysicalLink.Previous < 0)
            {
                // Relink the beginning of the chunk
                chunk.FirstBlockInPhysicalOrder = usedBlockIndex;
            }
            else
            {
                ref var previousBlock = ref GetBlockAt(freeBlock.PhysicalLink.Previous);
                previousBlock.PhysicalLink.Next = usedBlockIndex;
                Debug.Assert(previousBlock.OffsetIntoChunk + previousBlock.Size == offsetIntoChunk);
            }

            Debug.Assert(usedBlock.OffsetIntoChunk + size == freeBlock.OffsetIntoChunk);
            freeBlock.PhysicalLink.Previous = usedBlockIndex;

            // Move the free block to the new free-list location if necessary
            var newFirstLevelIndex = Mapping(freeBlock.Size, out var newSecondLevelIndex);
            if (firstLevelIndex != newFirstLevelIndex || secondLevelIndex != newSecondLevelIndex)
            {
                RemoveBlockFromFreeList(ref freeBlock, firstLevelIndex, secondLevelIndex);
                InsertBlockIntoFreeList(ref freeBlock, freeBlockIndex, newFirstLevelIndex, newSecondLevelIndex);
            }

            return new TlsfAllocation((uint)usedBlockIndex, (ulong)chunk.Info.BaseAddress + offsetIntoChunk, size);
        }
        else
        {
            // If we occupy the block entirely, let's mark it as used
            freeBlock.IsUsed = true;
            freeBlock.Size = size;

            RemoveBlockFromFreeList(ref freeBlock, firstLevelIndex, secondLevelIndex);

            chunk.UsedBlockCount++;
            chunk.FreeBlockCount--;
            Debug.Assert(chunk.FreeBlockCount >= 0);
            
            return new TlsfAllocation((uint)freeBlockIndex, (ulong)chunk.Info.BaseAddress + offsetIntoChunk, size);
        }
    }

    /// <summary>
    /// Frees an allocation.
    /// </summary>
    /// <param name="allocation">An allocation unit to free.</param>
    public void Free(TlsfAllocation allocation)
    {
        int blockIndex = (int)allocation.BlockIndex;
        ref var block = ref GetBlockAt(blockIndex);
        Debug.Assert(block.IsUsed);
        block.IsUsed = false;

        // Update statistics for the chunk
        ref var chunk = ref _chunks.UnsafeGetRefAt((int)block.ChunkIndex);
        chunk.TotalAllocated -= block.Size;
        chunk.UsedBlockCount--;
        chunk.FreeBlockCount++;
        Debug.Assert(chunk.UsedBlockCount >= 0);

        // Merge the block with the previous block if possible
        var previousBlockIndex = block.PhysicalLink.Previous;
        if (previousBlockIndex >= 0)
        {
            ref var previousBlock = ref GetBlockAt(previousBlockIndex);
            if (!previousBlock.IsUsed)
            {
                RemoveBlockFromFreeList(ref previousBlock, Mapping(previousBlock.Size, out var previousSecondLevelIndex), previousSecondLevelIndex);

                block.Size += previousBlock.Size;
                block.OffsetIntoChunk = previousBlock.OffsetIntoChunk;

                var previousPreviousBlockIndex = previousBlock.PhysicalLink.Previous;
                block.PhysicalLink.Previous = previousPreviousBlockIndex;

                if (previousPreviousBlockIndex < 0)
                {
                    // Relink the beginning of the chunk
                    chunk.FirstBlockInPhysicalOrder = blockIndex;
                }
                else
                {
                    // Link the 2 x previous block to the new block
                    GetBlockAt(previousPreviousBlockIndex).PhysicalLink.Next = blockIndex;
                    Debug.Assert(GetBlockAt(previousPreviousBlockIndex).IsUsed);
                }

                chunk.FreeBlockCount--;
                Debug.Assert(chunk.FreeBlockCount >= 0);
                MarkBlockAsAvailable(ref previousBlock, previousBlockIndex);
            }
        }

        // Merge the block with the next block if possible
        var nextBlockIndex = block.PhysicalLink.Next;
        if (nextBlockIndex >= 0)
        {
            ref var nextBlock = ref GetBlockAt(nextBlockIndex);
            if (!nextBlock.IsUsed)
            {
                RemoveBlockFromFreeList(ref nextBlock, Mapping(nextBlock.Size, out var nextSecondLevelIndex), nextSecondLevelIndex);
                block.Size += nextBlock.Size;
                var nextNextBlockIndex = nextBlock.PhysicalLink.Next;
                block.PhysicalLink.Next = nextNextBlockIndex;

                if (nextNextBlockIndex >= 0)
                {
                    // Link the block to the 2 x next block
                    GetBlockAt(nextNextBlockIndex).PhysicalLink.Previous = blockIndex;
                    Debug.Assert(GetBlockAt(nextNextBlockIndex).IsUsed);
                }

                chunk.FreeBlockCount--;
                Debug.Assert(chunk.FreeBlockCount >= 0);
                MarkBlockAsAvailable(ref nextBlock, nextBlockIndex);
            }
        }

        // Insert the block into the free list
        var firstLevelIndex = Mapping(block.Size, out var secondLevelIndex);
        InsertBlockIntoFreeList(ref block, blockIndex, firstLevelIndex, secondLevelIndex);
    }

    /// <summary>
    /// Free all the chunks allocated by this allocator and reset all the internal state.
    /// </summary>
    public void Reset()
    {
        for (int i = 0; i < _chunks.Count; i++)
        {
            _context.FreeChunk(_chunks[i].Info);
        }
        _chunks.Clear();
        _blocks.Clear();
        _indexToFirstAvailableBlock = -1;
        _bins = default;
        _bins.Initialize();
    }

    /// <summary>
    /// Dumps the internal state of this allocator to a string.
    /// </summary>
    /// <param name="buffer">The buffer to receive the dump.</param>
    public void Dump(StringBuilder buffer)
    {
        buffer.AppendLine("TLSF Allocator");
        buffer.AppendLine("==============");
        buffer.AppendLine();
        buffer.AppendLine($"Alignment: {_alignment}");
        buffer.AppendLine();
        buffer.AppendLine($"Chunks: {_chunks.Count,2}");
        buffer.AppendLine("----------");
        for (int i = 0; i < _chunks.Count; i++)
        {
            ref var chunk = ref _chunks[i];
            buffer.AppendLine($"Chunk {chunk.Info.Id}:");
            buffer.AppendLine($"  BaseAddress: {chunk.Info.BaseAddress}");
            buffer.AppendLine($"  Size: {chunk.Info.Size}");
            buffer.AppendLine($"  TotalAllocated: {chunk.TotalAllocated}");
            buffer.AppendLine($"  UsedBlockCount: {chunk.UsedBlockCount}");
            buffer.AppendLine($"  FreeBlockCount: {chunk.FreeBlockCount}");
            buffer.AppendLine($"  FirstBlockInPhysicalOrder: {chunk.FirstBlockInPhysicalOrder}");
            buffer.AppendLine();
        }

        if (_chunks.Count == 0)
        {
            buffer.AppendLine("No chunks allocated");
            buffer.AppendLine();
        }

        buffer.AppendLine($"Bins: 0b{ToBin(_bins.FirstLevelBitmap)}");
        buffer.AppendLine($"--------{new('-', 32)}");

        bool hasBins = false;
        for (int i = 0; i < BinCount; i++)
        {
            var firstLevelBinSizeStart = i == 0 ? 0 : (uint)(1 << (i + BaseBin0Log2 - 1));
            var firstLevelBinSizeEnd = i == 0 ? (uint)(1 << (i + BaseBin0Log2 - 1)) : (uint)(1 << (i + BaseBin0Log2));
            var sizeOfSecondLevelBin = (firstLevelBinSizeEnd - firstLevelBinSizeStart) >> SubBinsLog2;

            var mask2 = _bins.GetSecondLevelBitmap(i);
            if (mask2 != 0)
            {
                buffer.AppendLine($"Bin L1 ({i}): [0x{firstLevelBinSizeStart:x}, 0x{firstLevelBinSizeEnd - 1:x}[ -> Mask: 0b{ToBin(mask2)}");
                for (int j = 0; j < SubBinCount; j++)
                {
                    if (_bins.IsSecondLevelBitSet(i, j))
                    {
                        var secondLevelBinSizeStart = firstLevelBinSizeStart + j * sizeOfSecondLevelBin;
                        var secondLevelBinSizeEnd = secondLevelBinSizeStart + sizeOfSecondLevelBin;

                        buffer.AppendLine($"  Bin L2 ({j}): [0x{secondLevelBinSizeStart:x}, 0x{secondLevelBinSizeEnd - 1:x}[ -> FirstFreeBlock: {_bins.GetFirstFreeBlockIndexRefAt(i, j)}");
                    }
                }

                hasBins = true;
            }
        }

        if (!hasBins)
        {
            buffer.AppendLine("No bins allocated");
        }
        buffer.AppendLine();

        buffer.AppendLine($"Blocks: {_blocks.Count,3}");
        buffer.AppendLine("-----------");
        var availableBlocks = new HashSet<int>();
        var nextAvailableIndex = _indexToFirstAvailableBlock;
        while (nextAvailableIndex >= 0)
        {
            availableBlocks.Add(nextAvailableIndex);
            nextAvailableIndex = GetBlockAt(nextAvailableIndex).FreeLink.Next;
        }

        const int C1 = 6;
        const int C2 = 5;
        const int C3 = 6;
        const int C4 = 6;
        const int C5 = 6;
        const int C6 = 14;
        const int C7 = 14;

        buffer.AppendLine($"{"Block",C1} {"Chunk",C2} {"Offset",C3} {"Size", C4} {"Status", C5} {"Free Links", C6} {"Phys Links", C7}");

        int firstBlockAvailableIndex = -1;
        for (int i = 0; i < _blocks.Count; i++)
        {
            if (availableBlocks.Contains(i))
            {
                if (firstBlockAvailableIndex < 0)
                {
                    firstBlockAvailableIndex = i;
                }
            }
            else
            {
                if (firstBlockAvailableIndex >= 0)
                {
                    var length = i - firstBlockAvailableIndex;
                    buffer.AppendLine($"{$"[{(length == 1 ? firstBlockAvailableIndex : $"{firstBlockAvailableIndex}-{i-1}")}]",C1} {$"",C2} {"",C3} {"",C4} {"Avail",C5} {"",C6} {"",C7}");
                    firstBlockAvailableIndex = -1;
                }
                
                ref var block = ref _blocks[i];
                buffer.AppendLine($"{$"[{i}]",C1} {$"{block.ChunkIndex}",C2} {block.OffsetIntoChunk,C3} {block.Size,C4} {(block.IsUsed ? "Used" : "Free"),C5} {$"{block.FreeLink.Previous,3} <-> {block.FreeLink.Next,3}",C6} {$"{block.PhysicalLink.Previous,3} <-> {block.PhysicalLink.Next,3}",C7}");
            }
        }

        if (firstBlockAvailableIndex >= 0)
        {
            var length = _blocks.Count - firstBlockAvailableIndex;
            buffer.AppendLine($"{$"[{(length == 1 ? firstBlockAvailableIndex : $"{firstBlockAvailableIndex}-{_blocks.Count - 1}")}]",C1} {$"",C2} {"",C3} {"",C4} {"Avail",C5} {"",C6} {"",C7}");
        }

        if (_blocks.Count == 0)
        {
            buffer.AppendLine("No blocks allocated");
            buffer.AppendLine();
        }
        
        static string ToBin<T>(T number) where T : unmanaged, IBinaryInteger<T>
        {
            var builder = new StringBuilder();
            var size = sizeof(T) * 8;
            for (int i = size - 1; i >= 0; i--)
            {
                builder.Append((number & (T.One << i)) != T.Zero ? '1' : '0');
                if (i > 0 && i % 4 == 0)
                {
                    builder.Append('_');
                }
            }
            return builder.ToString();
        }
    }

    private int GetNextAvailableBlockIndex()
    {
        var index = _indexToFirstAvailableBlock;
        if (index < 0)
        {
            index = _blocks.Count; // next index
        }
        else
        {
            _indexToFirstAvailableBlock = GetBlockAt(index).FreeLink.Next;
        }
        return index;
    }

    private void MarkBlockAsAvailable(ref Block block, int blockIndex)
    {
        block = default;
        block.IsAvailable = true;
        block.FreeLink = BlockLinks.Undefined;
        block.PhysicalLink = BlockLinks.Undefined;

        var previousAvailableIndex = _indexToFirstAvailableBlock;
        _indexToFirstAvailableBlock = blockIndex;
        block.FreeLink.Next = previousAvailableIndex;
    }

    private ref Block GetBlockAt(int index)
    {
        Debug.Assert(index >= 0 && index < _blocks.Count);
        return ref _blocks.UnsafeGetRefAt(index);
    }

    private ref Block GetOrCreateBlockAt(int index)
    {
        return ref _blocks.UnsafeGetOrCreate(index);
    }
    
    private void RemoveBlockFromFreeList(ref Block block, int firstLevelIndex, int secondLevelIndex)
    {
        var previousBlockIndex = block.FreeLink.Previous;
        var nextBlockIndex = block.FreeLink.Next;

        if (previousBlockIndex < 0)
        {
            // If this block is the first block in the free list, we need to update the first free block index
            _bins.GetFirstFreeBlockIndexRefAt(firstLevelIndex, secondLevelIndex) = nextBlockIndex;
        }
        else
        {
            // Otherwise, we need to update the previous block next link to the next block
            ref var previousBlock = ref GetBlockAt(previousBlockIndex);
            previousBlock.FreeLink.Next = nextBlockIndex;
        }

        if (nextBlockIndex >= 0)
        {
            // If this block is not the last block in the free list, we need to update the next block previous link to the previous block
            ref var nextBlock = ref GetBlockAt(nextBlockIndex);
            nextBlock.FreeLink.Previous = previousBlockIndex;
        }
        else
        {
            // Clear bitmap if necessary
            if (previousBlockIndex < 0 && _bins.ClearSecondLevelBit(firstLevelIndex, secondLevelIndex))
            {
                // If a second level is empty, we need to clear the first level bit
                _bins.ClearFirstLevelBit(firstLevelIndex);
            }
        }
    }

    private void InsertBlockIntoFreeList(ref Block block, int blockIndex, int firstLevelIndex, int secondLevelIndex)
    {
        _bins.SetFirstLevelBit(firstLevelIndex);
        _bins.SetSecondLevelBit(firstLevelIndex, secondLevelIndex);

        ref var firstFreeIndex = ref _bins.GetFirstFreeBlockIndexRefAt(firstLevelIndex, secondLevelIndex);

        if (firstFreeIndex < 0)
        {
            block.FreeLink = BlockLinks.Undefined;
        }
        else
        {
            ref var previousBlock = ref GetBlockAt(firstFreeIndex);
            block.FreeLink.Previous = -1;
            block.FreeLink.Next = firstFreeIndex;
            Debug.Assert(previousBlock.FreeLink.Previous < 0);
            previousBlock.FreeLink.Previous = blockIndex;
        }
        firstFreeIndex = blockIndex;
    }

    private ref Block FindSuitableBlock(uint size, ref int firstLevelIndex, ref int secondLevelIndex, out int blockIndex)
    {
        findFirstLevel:
        firstLevelIndex = _bins.GetFirstLevelIndexAvailableAt(firstLevelIndex);

        // If we don't have a block in higher level directory, we need to allocate a new chunk
        if (firstLevelIndex < 0)
        {
            int chunkIndex = _chunks.Count;
            ref var chunkEntry = ref _chunks.UnsafeGetOrCreate(chunkIndex);
            chunkEntry = default;

            ref var chunk = ref chunkEntry.Info;
            blockIndex = GetNextAvailableBlockIndex();
            chunkEntry.FreeBlockCount++;
            chunkEntry.FirstBlockInPhysicalOrder = blockIndex;

            chunk = _context.AllocateChunk(new(size)); // We know that size is at minimum _alignment size
            Debug.Assert(BitOperations.IsPow2(chunk.Size));
            ref var block = ref GetOrCreateBlockAt(blockIndex);
            block.ChunkIndex = (uint)chunkIndex;
            // We align the offset to the alignment (so free blocks are always aligned)
            block.OffsetIntoChunk = AlignHelper.AlignUpOffset(chunk.BaseAddress, _alignment);
            block.Size = chunk.Size - block.OffsetIntoChunk;
            block.IsUsed = false;
            block.FreeLink = BlockLinks.Undefined;
            block.PhysicalLink = BlockLinks.Undefined;

            // Update the first level directory
            var firstLevelIndexChunk = Mapping(chunk.Size, out var localSecondLevelIndex);
            // Update the second level directory
            InsertBlockIntoFreeList(ref block, blockIndex, firstLevelIndexChunk, localSecondLevelIndex);

            firstLevelIndex = firstLevelIndexChunk;
            secondLevelIndex = localSecondLevelIndex;
            return ref block;
        }
        else
        {
            var localFirstLevelIndex = firstLevelIndex;
            var localSecondLevelIndex = secondLevelIndex;

            localSecondLevelIndex = _bins.GetSecondLevelIndexAvailableAt(localFirstLevelIndex, localSecondLevelIndex);

            // If we are failing to find a block in the second level directory, we need to move to the next first level directory
            // to 1) find an existing level directory with a free block or 2) allocate a new chunk
            if (localSecondLevelIndex < 0)
            {
                secondLevelIndex = 0;
                firstLevelIndex++;
                goto findFirstLevel;
            }

            firstLevelIndex = localFirstLevelIndex;
            secondLevelIndex = localSecondLevelIndex;

            blockIndex = _bins.GetFirstFreeBlockIndexRefAt(localFirstLevelIndex, localSecondLevelIndex);
            ref var block = ref GetBlockAt(blockIndex);

            // This case can happen if we have a block that is too small for the requested size
            // it still appears in the free list but the granularity of the 2nd level doesn't guarantee that the block is big enough
            // For example a free block might be of size 16384 - 128, and we ask for a block size of 16384 - 64
            // In both cases, the first level is 3, and the second level is 0, but the block with a size of (16384 - 128) is not enough for the requested size (16384 - 64)
            if (block.Size < size)
            {
                secondLevelIndex = 0;
                firstLevelIndex++;
                goto findFirstLevel;
            }
            return ref block;
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Mapping(uint size, out int secondLevelIndex)
    {
        int firstIndex;
        var value = size >> BaseBin0Log2;
        if (value == 0)
        {
            firstIndex = 0;
            secondLevelIndex = 0;
        }
        else
        {
            firstIndex = 31 - BitOperations.LeadingZeroCount(value);
            secondLevelIndex = (int)(value ^ (1 << firstIndex)) >> (firstIndex - SubBinsLog2);
        }
        return firstIndex;
    }

    private struct BinsDirectory
    {
        private uint _firstLevelBitmap;
        private fixed ushort _secondLevelBitmap[BinCount];
        private fixed int _firstFreeBlockIndices[TotalBinCount];

        public uint FirstLevelBitmap => _firstLevelBitmap;

        public ushort GetSecondLevelBitmap(int index) => _secondLevelBitmap[index];
        
        public BinsDirectory()
        {
            Initialize();
        }

        public void Initialize()
        {
            for (int i = 0; i < TotalBinCount; i++)
            {
                _firstFreeBlockIndices[i] = -1;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsFirstLevelBitSet(int index)
            => (_firstLevelBitmap & (1 << index)) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetFirstLevelIndexAvailableAt(int firstLevelIndex)
        {
            var mask = _firstLevelBitmap >> firstLevelIndex;
            return mask == 0 ? -1 : firstLevelIndex + BitOperations.TrailingZeroCount(mask);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetFirstLevelBit(int index)
            => _firstLevelBitmap |= 1U << index;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearFirstLevelBit(int index)
            => _firstLevelBitmap &= ~(1U << index);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ClearSecondLevelBit(int firstLevelIndex, int secondLevelIndex)
        {
            ref ushort secondLevel = ref _secondLevelBitmap[firstLevelIndex];
            var level = secondLevel & (ushort)~(1 << secondLevelIndex);
            secondLevel = (ushort)level;
            return level == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSecondLevelBitSet(int firstLevelIndex, int secondLevelIndex)
            => (_secondLevelBitmap[firstLevelIndex] & (1 << secondLevelIndex)) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetSecondLevelBit(int firstLevelIndex, int secondLevelIndex)
            => _secondLevelBitmap[firstLevelIndex] |= (ushort)(1 << secondLevelIndex);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetSecondLevelIndexAvailableAt(int firstLevelIndex, int secondLevelIndex)
        {
            var mask = _secondLevelBitmap[firstLevelIndex] >> secondLevelIndex;
            return mask == 0 ? -1 : secondLevelIndex + BitOperations.TrailingZeroCount(mask);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref int GetFirstFreeBlockIndexRefAt(int firstLevelIndex, int secondLevelIndex)
        {
            Debug.Assert(firstLevelIndex >= 0 && firstLevelIndex < BinCount);
            Debug.Assert(secondLevelIndex >= 0 && secondLevelIndex < SubBinCount);
            return ref Unsafe.Add(ref _firstFreeBlockIndices[0], firstLevelIndex * SubBinCount + secondLevelIndex);
        }
    }

    /// <summary>
    /// This defines a block in the TLSF allocator that is either used or free.
    /// </summary>
    /// <remarks>
    /// This structure is 32 bytes (6 x sizeof(uint))
    /// </remarks>
    [DebuggerDisplay("{ToDebuggerDisplay(),nq}")]
    private struct Block
    {
        /// <summary>
        /// Gets or sets the index of the associated chunk this block belongs to.
        /// </summary>
        public uint ChunkIndex;

        /// <summary>
        /// Gets or sets the offset into the chunk (relative to the base address).
        /// </summary>
        public uint OffsetIntoChunk;

        /// <summary>
        /// Gets or sets the size of this block.
        /// </summary>
        public uint Size;

        private uint _flags; // we could combine it into size, but it is more readable and we want to keep this structure 32 bytes.

        /// <summary>
        /// Previous and next link for the free list per second level bins.
        /// </summary>
        public BlockLinks FreeLink;

        /// <summary>
        /// Previous and next link for the physical order of the blocks in the chunk, the first block being referenced by <see cref="Chunk.FirstBlockInPhysicalOrder"/>.
        /// </summary>
        public BlockLinks PhysicalLink;

        [ExcludeFromCodeCoverage]
        private string ToDebuggerDisplay()
        {
            return $"Offset: {OffsetIntoChunk}, Size: {Size}, End: {OffsetIntoChunk + Size}, Status: {(IsUsed ? "Used":IsAvailable?"Avail":"Free")}, FreeLink: {FreeLink.Previous}<->{FreeLink.Next},  PhysicalLink: {PhysicalLink.Previous} <-> {PhysicalLink.Next}";
        }
        
        public bool IsUsed
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (_flags & 1) != 0;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _flags = value ? 1U : 0;
        }

        public bool IsAvailable
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (_flags & 2) != 0;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _flags = value ? 2U : 0;
        }
    }

    private struct BlockLinks
    {
        public static BlockLinks Undefined => Unsafe.BitCast<long, BlockLinks>(-1);

        public int Previous;
        public int Next;
    }

    /// <summary>
    /// The structure is internal because it is exposed via <see cref="TlsfChunk"/>.
    /// </summary>
    internal struct Chunk
    {
        /// <summary>
        /// Gets the associated chunk.
        /// </summary>
        public MemoryChunk Info;

        public MemorySize TotalAllocated;

        public int UsedBlockCount;

        public int FreeBlockCount;

        internal int FirstBlockInPhysicalOrder;
    }
}