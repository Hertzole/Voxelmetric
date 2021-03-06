﻿using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Voxelmetric
{
    public sealed class ChunkBlocks
    {
        public Chunk Chunk { get; private set; }

        private Block[] blockTypes;

        private readonly int sideSize = 0;
        private readonly int pow = 0;

        //! Array of block data
        private readonly IntPtr blocksRaw;
        private readonly unsafe byte* blocks;
        private unsafe BlockData this[int i]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return *((BlockData*)&blocks[i << 1]);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                *((BlockData*)&blocks[i << 1]) = value;
            }
        }

        //! Compressed array of block data
        private readonly List<BlockDataAABB> blockCompressed = new List<BlockDataAABB>();
        public List<BlockDataAABB> BlocksCompressed
        {
            get { return blockCompressed; }
        }

        //! Number of blocks which are not air (non-empty blocks)
        public int nonEmptyBlocks;

        private byte[] receiveBuffer;
        private int receiveIndex;

        public List<BlockPos> modifiedBlocks = new List<BlockPos>();

        private static byte[] emptyBytes;
        public static byte[] EmptyBytes
        {
            get
            {
                if (emptyBytes == null)
                {
                    emptyBytes = new byte[16384]; // TODO: Validate whether this is fine
                }

                return emptyBytes;
            }
        }

        public static int GetLength(int sideSize)
        {
            return sideSize * sideSize * sideSize;
        }

        public static int GetDataSize(int sideSize)
        {
            return GetLength(sideSize) * StructSerialization.TSSize<BlockData>.ValueSize;
        }

        public unsafe ChunkBlocks(Chunk chunk, int sideSize)
        {
            Chunk = chunk;

            this.sideSize = sideSize;
            pow = 1 + (int)Math.Log(sideSize, 2);

            sideSize = this.sideSize + Env.CHUNK_PADDING_2;

            // Allocate the memory aligned to 16B boundaries
            int arrLen = GetDataSize(sideSize);
            blocksRaw = Marshal.AllocHGlobal(arrLen + 8);
            IntPtr aligned = new IntPtr(16 * (((long)blocksRaw + 15) / 16));
            blocks = (byte*)aligned.ToPointer();
            Utils.ZeroMemory(blocks, arrLen);
        }

        ~ChunkBlocks()
        {
            Marshal.FreeHGlobal(blocksRaw);
        }

        public void Init()
        {
            blockTypes = Chunk.World ? Chunk.World.blockProvider.BlockTypes : null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Copy(ChunkBlocks src, int srcIndex, int dstIndex, int length)
        {
            Utils.MemoryCopy(&blocks[dstIndex << 1], &src.blocks[srcIndex << 1], (uint)length << 1);
        }

        public unsafe void Reset()
        {
            nonEmptyBlocks = -1;

            // Reset internal parts of the chunk buffer
            int sideSize = this.sideSize + Env.CHUNK_PADDING_2;
            Utils.ZeroMemory(blocks, sideSize * sideSize * sideSize * StructSerialization.TSSize<BlockData>.ValueSize);

            // We have to reallocate the list. Otherwise, the array could potentially grow
            // to Env.ChunkSizePow3 size.
            if (modifiedBlocks == null || modifiedBlocks.Count > this.sideSize * 3) // Reallocation threshold
            {
                modifiedBlocks = new List<BlockPos>();
            }
            else
            {
                modifiedBlocks.Clear();
            }
        }

        public void CalculateEmptyBlocks()
        {
            if (nonEmptyBlocks >= 0)
            {
                return;
            }

            nonEmptyBlocks = 0;

            int sizeWithPadding = sideSize + Env.CHUNK_PADDING_2;
            int sizeWithPaddingPow2 = sizeWithPadding * sizeWithPadding;

            int index = Env.CHUNK_PADDING + (Env.CHUNK_PADDING << pow) + (Env.CHUNK_PADDING << (pow << 1));
            int yOffset = sizeWithPaddingPow2 - sideSize * sizeWithPadding;
            int zOffset = sizeWithPadding - sideSize;
            for (int y = 0; y < sideSize; ++y, index += yOffset)
            {
                for (int z = 0; z < sideSize; ++z, index += zOffset)
                {
                    for (int x = 0; x < sideSize; ++x, ++index)
                    {
                        if (this[index].Type != BlockProvider.AIR_TYPE)
                        {
                            ++nonEmptyBlocks;
                        }
                    }
                }
            }
        }

        public void ProcessSetBlock(BlockData blockData, int index, bool setBlockModified)
        {
            // Nothing for us to do if there was no change
            BlockData oldBlockData = this[index];
            if (oldBlockData.Type == blockData.Type)
            {
                return;
            }

            Helpers.GetChunkIndex3DFrom1D(index, out int x, out int y, out int z, pow);
            Vector3Int pos = new Vector3Int(x, y, z);

            // Handle block specific events
            Block oldBlock = blockTypes[oldBlockData.Type];
            Block newBlock = blockTypes[blockData.Type];
            oldBlock.OnDestroy(Chunk, ref pos);
            newBlock.OnCreate(Chunk, ref pos);

            // Update chunk status
            if (blockData.Type == BlockProvider.AIR_TYPE)
            {
                --nonEmptyBlocks;
            }
            else if (oldBlockData.Type == BlockProvider.AIR_TYPE)
            {
                ++nonEmptyBlocks;
            }

            // Update block info
            this[index] = blockData;

            // Notify about modification
            if (setBlockModified)
            {
                Vector3Int globalPos = pos + Chunk.Pos;
                BlockModified(new BlockPos(pos.x, pos.y, pos.z), ref globalPos, blockData);
            }
        }

        /// <summary>
        /// Returns block data from a position within the chunk
        /// </summary>
        /// <param name="pos">Position in local chunk coordinates</param>
        /// <returns>The block at the position</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BlockData Get(ref Vector3Int pos)
        {
            int index = Helpers.GetChunkIndex1DFrom3D(pos.x, pos.y, pos.z, pow);
            return this[index];
        }

        /// <summary>
        /// Returns a block from a position within the chunk
        /// </summary>
        /// <param name="pos">Position in local chunk coordinates</param>
        /// <returns>The block at the position</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Block GetBlock(ref Vector3Int pos)
        {
            int index = Helpers.GetChunkIndex1DFrom3D(pos.x, pos.y, pos.z, pow);
            return blockTypes[this[index].Type];
        }

        /// <summary>
        /// Returns block data from a position within the chunk
        /// </summary>
        /// <param name="index">Index in local chunk data</param>
        /// <returns>The block at the position</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BlockData Get(int index)
        {
            return this[index];
        }

        /// <summary>
        /// Returns a block from a position within the chunk
        /// </summary>
        /// <param name="index">Index in local chunk data</param>
        /// <returns>The block at the position</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Block GetBlock(int index)
        {
            return blockTypes[this[index].Type];
        }

        /// <summary>
        /// Sets the block at the given position. The position is guaranteed to be inside chunk's non-padded area
        /// </summary>
        /// <param name="index">Index in local chunk data</param>
        /// <param name="blockData">A block to be placed on a given position</param>
        public void SetInner(int index, BlockData blockData)
        {
            // Nothing for us to do if there was no change
            BlockData oldBlockData = this[index];
            ushort type = blockData.Type;
            if (oldBlockData.Type == type)
            {
                return;
            }

            if (type == BlockProvider.AIR_TYPE)
            {
                --nonEmptyBlocks;
            }
            else if (oldBlockData.Type == BlockProvider.AIR_TYPE)
            {
                ++nonEmptyBlocks;
            }

            this[index] = blockData;
        }

        /// <summary>
        /// Sets the block at the given position. It does not perform any logic. It simply sets the block.
        /// Use this function only when generating the terrain and structures.
        /// </summary>
        /// <param name="index">Index in local chunk data</param>
        /// <param name="blockData">A block to be placed on a given position</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetRaw(int index, BlockData blockData)
        {
            this[index] = blockData;
        }

        /// <summary>
        /// Sets blocks to a given value in a given range
        /// </summary>
        /// <param name="posFrom">Starting position in local chunk coordinates</param>
        /// <param name="posTo">Ending position in local chunk coordinates</param>
        /// <param name="blockData">A block to be placed on a given position</param>
        public void SetRange(ref Vector3Int posFrom, ref Vector3Int posTo, BlockData blockData)
        {
            int sizeWithPadding = sideSize + Env.CHUNK_PADDING_2;
            int sizeWithPaddingPow2 = sizeWithPadding * sizeWithPadding;

            int index = Helpers.GetChunkIndex1DFrom3D(posFrom.x, posFrom.y, posFrom.z, pow);
            int yOffset = sizeWithPaddingPow2 - (posTo.z - posFrom.z + 1) * sizeWithPadding;
            int zOffset = sizeWithPadding - (posTo.x - posFrom.x + 1);

            for (int y = posFrom.y; y <= posTo.y; ++y, index += yOffset)
            {
                for (int z = posFrom.z; z <= posTo.z; ++z, index += zOffset)
                {
                    for (int x = posFrom.x; x <= posTo.x; ++x, ++index)
                    {
                        SetInner(index, blockData);
                    }
                }
            }
        }

        /// <summary>
        /// Sets blocks to a given value in a given range. It does not perform any logic. It simply sets the blocks.
        /// Use this function only when generating the terrain and structures.
        /// </summary>
        /// <param name="posFrom">Starting position in local chunk coordinates</param>
        /// <param name="posTo">Ending position in local chunk coordinates</param>
        /// <param name="blockData">A block to be placed on a given position</param>
        public void SetRangeRaw(ref Vector3Int posFrom, ref Vector3Int posTo, BlockData blockData)
        {
            int sizeWithPadding = sideSize + Env.CHUNK_PADDING_2;
            int sizeWithPaddingPow2 = sizeWithPadding * sizeWithPadding;

            int index = Helpers.GetChunkIndex1DFrom3D(posFrom.x, posFrom.y, posFrom.z, pow);
            int yOffset = sizeWithPaddingPow2 - (posTo.z - posFrom.z + 1) * sizeWithPadding;
            int zOffset = sizeWithPadding - (posTo.x - posFrom.x + 1);

            for (int y = posFrom.y; y <= posTo.y; ++y, index += yOffset)
            {
                for (int z = posFrom.z; z <= posTo.z; ++z, index += zOffset)
                {
                    for (int x = posFrom.x; x <= posTo.x; ++x, ++index)
                    {
                        SetRaw(index, blockData);
                    }
                }
            }
        }

        public void BlockModified(BlockPos blockPos, ref Vector3Int globalPos, BlockData blockData)
        {
            VmNetworking ntw = Chunk.World.networking;

            // If this is the server log the changed block so that it can be saved
            if (ntw.isServer)
            {
                if (ntw.allowConnections)
                {
                    ntw.server.BroadcastChange(globalPos, blockData, -1);
                }

                if (Features.useDifferentialSerialization)
                {
                    // TODO: Memory unfriendly. Rethink the strategy
                    modifiedBlocks.Add(blockPos);
                }
            }
            else // if this is not the server send the change to the server to sync
            {
                ntw.client.BroadcastChange(globalPos, blockData);
            }
        }

        private void InitializeChunkDataReceive(int index, int size)
        {
            receiveIndex = index;
            receiveBuffer = new byte[size];
        }

        public void ReceiveChunkData(byte[] buffer)
        {
            int index = BitConverter.ToInt32(buffer, VmServer.HEADER_SIZE);
            int size = BitConverter.ToInt32(buffer, VmServer.HEADER_SIZE + 4);

            if (receiveBuffer == null)
            {
                InitializeChunkDataReceive(index, size);
            }

            TranscribeChunkData(buffer, VmServer.LEADER_SIZE);
        }

        private void TranscribeChunkData(byte[] buffer, int offset)
        {
            for (int o = offset; o < buffer.Length; o++)
            {
                receiveBuffer[receiveIndex] = buffer[o];
                receiveIndex++;

                if (receiveIndex == receiveBuffer.Length)
                {
                    FinishChunkDataReceive();
                    return;
                }
            }
        }

        private void FinishChunkDataReceive()
        {
            if (!InitFromBytes())
            {
                Reset();
            }

            Chunk.OnGenerateDataOverNetworkDone(Chunk);

            receiveBuffer = null;
            receiveIndex = 0;
        }

        #region Compression

        private bool ExpandX(ref bool[] mask, ushort type, int y1, int z1, ref int x2, int y2, int z2)
        {
            int sizeWithPadding = sideSize + Env.CHUNK_PADDING_2;
            int sizeWithPaddingPow2 = sizeWithPadding * sizeWithPadding;

            int yOffset = sizeWithPaddingPow2 - (z2 - z1) * sizeWithPadding;
            int index0 = Helpers.GetChunkIndex1DFrom3D(x2, y1, z1, pow);

            // Check the quad formed by YZ axes and try to expand the X asix
            int index = index0;
            for (int y = y1; y < y2; ++y, index += yOffset)
            {
                for (int z = z1; z < z2; ++z, index += sizeWithPadding)
                {
                    if (mask[index] || this[index].Type != type)
                    {
                        return false;
                    }
                }
            }

            // If the box can expand, mark the position as tested and expand the X axis
            index = index0;
            for (int y = y1; y < y2; ++y, index += yOffset)
            {
                for (int z = z1; z < z2; ++z, index += sizeWithPadding)
                {
                    mask[index] = true;
                }
            }

            ++x2;
            return true;
        }

        private bool ExpandY(ref bool[] mask, ushort type, int x1, int z1, int x2, ref int y2, int z2)
        {
            int sizeWithPadding = sideSize + Env.CHUNK_PADDING_2;

            int zOffset = sizeWithPadding - x2 + x1;
            int index0 = Helpers.GetChunkIndex1DFrom3D(x1, y2, z1, pow);

            // Check the quad formed by XZ axes and try to expand the Y axis
            int index = index0;
            for (int z = z1; z < z2; ++z, index += zOffset)
            {
                for (int x = x1; x < x2; ++x, ++index)
                {
                    if (mask[index] || this[index].Type != type)
                    {
                        return false;
                    }
                }
            }

            // If the box can expand, mark the position as tested and expand the X axis
            index = index0;
            for (int z = z1; z < z2; ++z, index += zOffset)
            {
                for (int x = x1; x < x2; ++x, ++index)
                {
                    mask[index] = true;
                }
            }

            ++y2;
            return true;
        }

        private bool ExpandZ(ref bool[] mask, ushort type, int x1, int y1, int x2, int y2, ref int z2)
        {
            int sizeWithPadding = sideSize + Env.CHUNK_PADDING_2;
            int sizeWithPaddingPow2 = sizeWithPadding * sizeWithPadding;

            int yOffset = sizeWithPaddingPow2 - x2 + x1;
            int index0 = Helpers.GetChunkIndex1DFrom3D(x1, y1, z2, pow);

            // Check the quad formed by XY axes and try to expand the Z axis
            int index = index0;
            for (int y = y1; y < y2; ++y, index += yOffset)
            {
                for (int x = x1; x < x2; ++x, ++index)
                {
                    if (mask[index] || this[index].Type != type)
                    {
                        return false;
                    }
                }
            }

            // If the box can expand, mark the position as tested and expand the X axis
            index = index0;
            for (int y = y1; y < y2; ++y, index += yOffset)
            {
                for (int x = x1; x < x2; ++x, ++index)
                {
                    mask[index] = true;
                }
            }

            ++z2;
            return true;
        }

        /// <summary>
        /// Compresses chunk's memory.
        /// </summary>
        public void Compress()
        {
            int sizePlusPadding = sideSize + Env.CHUNK_PADDING;
            int sizeWithPadding = sideSize + Env.CHUNK_PADDING_2;
            int sizeWithPaddingPow2 = sizeWithPadding * sizeWithPadding;
            int sizeWithPaddingPow3 = sizeWithPaddingPow2 * sizeWithPadding;

            LocalPools pools = Globals.WorkPool.GetPool(Chunk.ThreadID);
            bool[] mask = pools.boolArrayPool.PopExact(sizeWithPaddingPow3);

            Array.Clear(mask, 0, mask.Length);
            blockCompressed.Clear();

            // This compression is essentialy RLE. However, instead of working on 1 axis
            // it works in 3 dimensions.
            int index = 0;
            for (int y = -1; y < sizePlusPadding; ++y)
            {
                for (int z = -1; z < sizePlusPadding; ++z)
                {
                    for (int x = -1; x < sizePlusPadding; ++x, ++index)
                    {
                        // Skip already checked blocks
                        if (mask[index])
                        {
                            continue;
                        }

                        mask[index] = true;

                        // Skip air data
                        ushort data = this[index].Data;
                        ushort type = (ushort)(data & BlockData.typeMask);
                        if (type == BlockProvider.AIR_TYPE)
                        {
                            continue;
                        }

                        int x1 = x, y1 = y, z1 = z, x2 = x + 1, y2 = y + 1, z2 = z + 1;

                        bool expandX = true;
                        bool expandY = true;
                        bool expandZ = true;
                        bool expand;

                        // Try to expand our box in all axes
                        do
                        {
                            expand = false;

                            if (expandY)
                            {
                                expandY = y2 < sizePlusPadding && ExpandY(ref mask, type, x1, z1, x2, ref y2, z2);
                                expand = expandY;
                            }
                            if (expandZ)
                            {
                                expandZ = z2 < sizePlusPadding && ExpandZ(ref mask, type, x1, y1, x2, y2, ref z2);
                                expand |= expandZ;
                            }
                            if (expandX)
                            {
                                expandX = x2 < sizePlusPadding && ExpandX(ref mask, type, y1, z1, ref x2, y2, z2);
                                expand |= expandX;
                            }
                        } while (expand);

                        blockCompressed.Add(new BlockDataAABB(data, x1, y1, z1, x2, y2, z2));

                        /*// Let's make sure that we don't take too much space
                        int compressedSize = blockCompressed.Count*StructSerialization.TSSize<BlockDataAABB>.ValueSize;
                        int decompressedSize = sizeWithPaddingPow3*StructSerialization.TSSize<BlockData>.ValueSize;
                        if (compressedSize>=(decompressedSize>>1))
                        {
                            blockCompressed.Clear();
                            pools.BoolArrayPool.Push(mask);
                            return;
                        }*/
                    }
                }
            }

            pools.boolArrayPool.Push(mask);
        }

        /// <summary>
        /// Decompresses chunk's memory.
        /// </summary>
        public void Decompress()
        {
            int sizeWithPadding = sideSize + Env.CHUNK_PADDING_2;
            int sizeWithPaddingPow2 = sizeWithPadding * sizeWithPadding;

            for (int i = 0; i < blockCompressed.Count; i++)
            {
                BlockDataAABB box = blockCompressed[i];
                int x1 = box.MinX;
                int y1 = box.MinY;
                int z1 = box.MinZ;
                int x2 = box.MaxX;
                int y2 = box.MaxY;
                int z2 = box.MaxZ;
                ushort data = box.Data;

                int index = Helpers.GetChunkIndex1DFrom3D(x1, y1, z1, pow);
                int yOffset = sizeWithPaddingPow2 - (z2 - z1) * sizeWithPadding;
                int zOffset = sizeWithPadding - (x2 - x1);
                for (int y = y1; y < y2; ++y, index += yOffset)
                {
                    for (int z = z1; z < z2; ++z, index += zOffset)
                    {
                        for (int x = x1; x < x2; ++x, ++index)
                        {
                            this[index] = new BlockData(data);
                        }
                    }
                }
            }
        }

        #endregion

        #region "Temporary network compression solution"

        public byte[] ToBytes()
        {
            List<byte> buffer = new List<byte>();
            buffer.AddRange(BitConverter.GetBytes(nonEmptyBlocks));
            if (nonEmptyBlocks > 0)
            {
                int sameBlockCount = 1;
                BlockData lastBlockData = this[0];

                int sizeWithPadding = sideSize + Env.CHUNK_PADDING_2;
                int sizeWithPaddingPow3 = sizeWithPadding * sizeWithPadding * sizeWithPadding;

                for (int index = 1; index < sizeWithPaddingPow3; ++index)
                {
                    if (this[index].Equals(lastBlockData))
                    {
                        // If this is the same as the last block added increase the count
                        ++sameBlockCount;
                    }
                    else
                    {
                        buffer.AddRange(BitConverter.GetBytes(sameBlockCount));
                        buffer.AddRange(BlockData.ToByteArray(lastBlockData));

                        sameBlockCount = 1;
                        lastBlockData = this[index];
                    }
                }

                buffer.AddRange(BitConverter.GetBytes(sameBlockCount));
                buffer.AddRange(BlockData.ToByteArray(lastBlockData));
            }

            return buffer.ToArray();
        }

        private bool InitFromBytes()
        {
            if (receiveBuffer == null || receiveBuffer.Length < 4)
            {
                return false;
            }

            nonEmptyBlocks = BitConverter.ToInt32(receiveBuffer, 0);
            if (nonEmptyBlocks > 0)
            {
                int dataOffset = 4;
                int blockOffset = 0;

                while (dataOffset < receiveBuffer.Length)
                {
                    int sameBlockCount = BitConverter.ToInt32(receiveBuffer, dataOffset + 4); // 4 bytes
                    BlockData bd = new BlockData(BlockData.RestoreBlockData(receiveBuffer, dataOffset + 8)); // 2 bytes
                    for (int i = blockOffset; i < blockOffset + sameBlockCount; i++)
                    {
                        this[i] = bd;
                    }

                    dataOffset += 4 + 2;
                    blockOffset += sameBlockCount;
                }
            }

            return true;
        }

        #endregion
    }
}
