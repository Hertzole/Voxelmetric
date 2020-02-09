﻿using System;

namespace Voxelmetric
{
    public partial class World
    {
        /// <summary>
        /// Gets the chunk and retrives the block data at the given coordinates
        /// </summary>
        /// <param name="pos">Global position of the block data</param>
        public BlockData GetBlockData(ref Vector3Int pos)
        {
            // Transform the position into chunk coordinates
            Vector3Int chunkPos = Helpers.ContainingChunkPos(ref pos);

            // Return air for chunk that do not exist
            Chunk chunk = GetChunk(ref chunkPos);
            if (chunk == null)
            {
                return BlockProvider.airBlock;
            }

            int xx = Helpers.Mod(pos.x, Env.CHUNK_SIZE);
            int yy = Helpers.Mod(pos.y, Env.CHUNK_SIZE);
            int zz = Helpers.Mod(pos.z, Env.CHUNK_SIZE);

            return chunk.Blocks.Get(Helpers.GetChunkIndex1DFrom3D(xx, yy, zz));
        }

        public BlockData GetBlockData(Vector3Int pos)
        {
            // Transform the position into chunk coordinates
            Vector3Int chunkPos = Helpers.ContainingChunkPos(ref pos);

            Chunk chunk = GetChunk(ref chunkPos);
            if (chunk == null)
            {
                // Return air if the chunk that do not exist
                return BlockProvider.airBlock;
            }

            int xx = Helpers.Mod(pos.x, Env.CHUNK_SIZE);
            int yy = Helpers.Mod(pos.y, Env.CHUNK_SIZE);
            int zz = Helpers.Mod(pos.z, Env.CHUNK_SIZE);

            return chunk.Blocks.Get(Helpers.GetChunkIndex1DFrom3D(xx, yy, zz));
        }

        /// <summary>
        /// Retrives the block at given world coordinates
        /// </summary>
        /// <param name="pos">Global position of the block</param>
        public Block GetBlock(ref Vector3Int pos)
        {
            // Transform the position into chunk coordinates
            Vector3Int chunkPos = Helpers.ContainingChunkPos(ref pos);

            Chunk chunk = GetChunk(ref chunkPos);
            if (chunk == null)
            {
                // Return air if the chunk that do not exist
                return blockProvider.BlockTypes[BlockProvider.AIR_TYPE];
            }

            int xx = Helpers.Mod(pos.x, Env.CHUNK_SIZE);
            int yy = Helpers.Mod(pos.y, Env.CHUNK_SIZE);
            int zz = Helpers.Mod(pos.z, Env.CHUNK_SIZE);

            BlockData blockData = chunk.Blocks.Get(Helpers.GetChunkIndex1DFrom3D(xx, yy, zz));
            return blockProvider.BlockTypes[blockData.Type];
        }

        /// <summary>
        /// Sets the block data at given world coordinates
        /// </summary>
        /// <param name="pos">Global position of the block</param>
        /// <param name="blockData">A block to be placed on a given position</param>
        public void SetBlockData(ref Vector3Int pos, BlockData blockData)
        {
            // Transform the position into chunk coordinates
            Vector3Int chunkPos = Helpers.ContainingChunkPos(ref pos);

            Chunk chunk = GetChunk(ref chunkPos);
            if (chunk == null)
            {
                return;
            }

            int xx = Helpers.Mod(pos.x, Env.CHUNK_SIZE);
            int yy = Helpers.Mod(pos.y, Env.CHUNK_SIZE);
            int zz = Helpers.Mod(pos.z, Env.CHUNK_SIZE);

            chunk.Blocks.SetInner(Helpers.GetChunkIndex1DFrom3D(xx, yy, zz), blockData);
        }

        /// <summary>
        /// Sets the block data at given world coordinates. It does not perform any logic. It simply sets the block.
        /// Use this function only when generating the terrain or structures.
        /// </summary>
        /// <param name="pos">Global position of the block</param>
        /// <param name="blockData">A block to be placed on a given position</param>
        public void SetBlockDataRaw(ref Vector3Int pos, BlockData blockData)
        {
            // Transform the position into chunk coordinates
            Vector3Int chunkPos = Helpers.ContainingChunkPos(ref pos);

            Chunk chunk = GetChunk(ref chunkPos);
            if (chunk == null)
            {
                return;
            }

            int xx = Helpers.Mod(pos.x, Env.CHUNK_SIZE);
            int yy = Helpers.Mod(pos.y, Env.CHUNK_SIZE);
            int zz = Helpers.Mod(pos.z, Env.CHUNK_SIZE);

            chunk.Blocks.SetRaw(Helpers.GetChunkIndex1DFrom3D(xx, yy, zz), blockData);
        }

        /// <summary>
        /// Sets blocks to a given value in a given range
        /// </summary>
        /// <param name="posFrom">Starting position in local chunk coordinates</param>
        /// <param name="posTo">Ending position in local chunk coordinates</param>
        /// <param name="blockData">A block to be placed on a given position</param>
        public void SetBlockDataRanged(ref Vector3Int posFrom, ref Vector3Int posTo, BlockData blockData)
        {
            // Let's make sure that ranges are okay
            if (posFrom.x > posTo.x || posFrom.y > posTo.y || posFrom.z > posTo.z)
            {
                return;
            }

            // Transform positions into chunk coordinates
            Vector3Int chunkPosFrom = Helpers.ContainingChunkPos(ref posFrom);
            Vector3Int chunkPosTo = Helpers.ContainingChunkPos(ref posTo);

            // Update all chunks in range
            int minY = Helpers.Mod(posFrom.y, Env.CHUNK_SIZE);

            for (int cy = chunkPosFrom.y; cy <= chunkPosTo.y; cy += Env.CHUNK_SIZE, minY = 0)
            {
                int maxY = Math.Min(posTo.y - cy, Env.CHUNK_SIZE_1);
                int minZ = Helpers.Mod(posFrom.z, Env.CHUNK_SIZE);

                for (int cz = chunkPosFrom.z; cz <= chunkPosTo.z; cz += Env.CHUNK_SIZE, minZ = 0)
                {
                    int maxZ = Math.Min(posTo.z - cz, Env.CHUNK_SIZE_1);
                    int minX = Helpers.Mod(posFrom.x, Env.CHUNK_SIZE);

                    for (int cx = chunkPosFrom.x; cx <= chunkPosTo.x; cx += Env.CHUNK_SIZE, minX = 0)
                    {
                        Vector3Int chunkPos = new Vector3Int(cx, cy, cz);
                        Chunk chunk = GetChunk(ref chunkPos);
                        if (chunk == null)
                        {
                            continue;
                        }

                        int maxX = Math.Min(posTo.x - cx, Env.CHUNK_SIZE_1);

                        Vector3Int from = new Vector3Int(minX, minY, minZ);
                        Vector3Int to = new Vector3Int(maxX, maxY, maxZ);
                        chunk.Blocks.SetRange(ref from, ref to, blockData);
                    }
                }
            }
        }

        /// <summary>
        /// Sets blocks to a given value in a given range. It does not perform any logic. It simply sets the blocks.
        /// Use this function only when generating the terrain or structures.
        /// </summary>
        /// <param name="posFrom">Starting position in local chunk coordinates</param>
        /// <param name="posTo">Ending position in local chunk coordinates</param>
        /// <param name="blockData">A block to be placed on a given position</param>
        public void SetBlockDataRangedRaw(ref Vector3Int posFrom, ref Vector3Int posTo, BlockData blockData)
        {
            // Let's make sure that ranges are okay
            if (posFrom.x > posTo.x || posFrom.y > posTo.y || posFrom.z > posTo.z)
            {
                return;
            }

            // Transform positions into chunk coordinates
            Vector3Int chunkPosFrom = Helpers.ContainingChunkPos(ref posFrom);
            Vector3Int chunkPosTo = Helpers.ContainingChunkPos(ref posTo);

            // Update all chunks in range
            int minY = Helpers.Mod(posFrom.y, Env.CHUNK_SIZE);

            for (int cy = chunkPosFrom.y; cy <= chunkPosTo.y; cy += Env.CHUNK_SIZE, minY = 0)
            {
                int maxY = Math.Min(posTo.y - cy, Env.CHUNK_SIZE_1);
                int minZ = Helpers.Mod(posFrom.z, Env.CHUNK_SIZE);

                for (int cz = chunkPosFrom.z; cz <= chunkPosTo.z; cz += Env.CHUNK_SIZE, minZ = 0)
                {
                    int maxZ = Math.Min(posTo.z - cz, Env.CHUNK_SIZE_1);
                    int minX = Helpers.Mod(posFrom.x, Env.CHUNK_SIZE);

                    for (int cx = chunkPosFrom.x; cx <= chunkPosTo.x; cx += Env.CHUNK_SIZE, minX = 0)
                    {
                        Vector3Int chunkPos = new Vector3Int(cx, cy, cz);
                        Chunk chunk = GetChunk(ref chunkPos);
                        if (chunk == null)
                        {
                            continue;
                        }

                        int maxX = Math.Min(posTo.x - cx, Env.CHUNK_SIZE_1);

                        Vector3Int from = new Vector3Int(minX, minY, minZ);
                        Vector3Int to = new Vector3Int(maxX, maxY, maxZ);
                        chunk.Blocks.SetRangeRaw(ref from, ref to, blockData);
                    }
                }
            }
        }

        /// <summary>
        /// Sets the block data at given world coordinates, updates the chunk and its
        /// neighbors if the Update chunk flag is true or not set.
        /// </summary>
        /// <param name="pos">Global position of the block</param>
        /// <param name="blockData">The block be placed</param>
        /// <param name="setBlockModified">Set to true to mark chunk data as modified</param>
        /// <param name="onModified">Action to perform once the operation finished</param>
        public void ModifyBlockData(ref Vector3Int pos, BlockData blockData, bool setBlockModified,
            Action<ModifyBlockContext> onModified = null)
        {
            // Transform the position into chunk coordinates
            Vector3Int chunkPos = Helpers.ContainingChunkPos(ref pos);

            Chunk chunk = GetChunk(ref chunkPos);
            if (chunk == null)
            {
                return;
            }

            int index = Helpers.GetChunkIndex1DFrom3D(
                Helpers.Mod(pos.x, Env.CHUNK_SIZE),
                Helpers.Mod(pos.y, Env.CHUNK_SIZE),
                Helpers.Mod(pos.z, Env.CHUNK_SIZE)
                );

            // Nothing for us to do if the block did not change
            BlockData oldBlockData = chunk.Blocks.Get(index);
            if (oldBlockData.Type == blockData.Type)
            {
                return;
            }

            ModifyBlockContext context = null;
            if (onModified != null)
            {
                context = new ModifyBlockContext(onModified, this, index, index, blockData, setBlockModified);
            }

            chunk.Modify(new ModifyOpBlock(blockData, index, setBlockModified, context));
        }

        /// <summary>
        /// Queues a modification of blocks in a given range
        /// </summary>
        /// <param name="posFrom">Starting positon in local chunk coordinates</param>
        /// <param name="posTo">Ending position in local chunk coordinates</param>
        /// <param name="blockData">BlockData to place at the given location</param>
        /// <param name="setBlockModified">Set to true to mark chunk data as modified</param>
        /// <param name="onModified">Action to perform once the operation finished</param>
        public void ModifyBlockDataRanged(ref Vector3Int posFrom, ref Vector3Int posTo, BlockData blockData, bool setBlockModified,
            Action<ModifyBlockContext> onModified = null)
        {
            // Make sure the values are okay.
            FixValues(ref posFrom.x, ref posTo.x);
            FixValues(ref posFrom.y, ref posTo.y);
            FixValues(ref posFrom.z, ref posTo.z);

            Vector3Int chunkPosFrom = Helpers.ContainingChunkPos(ref posFrom);
            Vector3Int chunkPosTo = Helpers.ContainingChunkPos(ref posTo);

            ModifyBlockContext context = null;
            if (onModified != null)
            {
                context = new ModifyBlockContext(onModified, this,
                                                 Helpers.GetChunkIndex1DFrom3D(posFrom.x, posFrom.y, posFrom.z),
                                                 Helpers.GetChunkIndex1DFrom3D(posTo.x, posTo.y, posTo.z),
                                                 blockData, setBlockModified);
            }

            // Update all chunks in range
            int minY = Helpers.Mod(posFrom.y, Env.CHUNK_SIZE);

            for (int cy = chunkPosFrom.y; cy <= chunkPosTo.y; cy += Env.CHUNK_SIZE, minY = 0)
            {
                int maxY = Math.Min(posTo.y - cy, Env.CHUNK_SIZE_1);
                int minZ = Helpers.Mod(posFrom.z, Env.CHUNK_SIZE);

                for (int cz = chunkPosFrom.z; cz <= chunkPosTo.z; cz += Env.CHUNK_SIZE, minZ = 0)
                {
                    int maxZ = Math.Min(posTo.z - cz, Env.CHUNK_SIZE_1);
                    int minX = Helpers.Mod(posFrom.x, Env.CHUNK_SIZE);

                    for (int cx = chunkPosFrom.x; cx <= chunkPosTo.x; cx += Env.CHUNK_SIZE, minX = 0)
                    {
                        Vector3Int chunkPos = new Vector3Int(cx, cy, cz);
                        Chunk chunk = GetChunk(ref chunkPos);
                        if (chunk == null)
                        {
                            continue;
                        }

                        int maxX = Math.Min(posTo.x - cx, Env.CHUNK_SIZE_1);

                        chunk.Modify(
                            new ModifyOpCuboid(
                                blockData,
                                new Vector3Int(minX, minY, minZ),
                                new Vector3Int(maxX, maxY, maxZ),
                                setBlockModified,
                                context)
                            );
                    }
                }
            }
        }

        private void FixValues(ref int start, ref int end)
        {
            if (start > end)
            {
                int temp = start;
                start = end;
                end = temp;
            }
        }
    }
}
