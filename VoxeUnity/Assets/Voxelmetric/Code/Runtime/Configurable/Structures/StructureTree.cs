﻿using UnityEngine.Scripting;

namespace Voxelmetric
{
    public class StructureContextTreeCrown : StructureContext
    {
        public Vector3Int worldPos;
        public Vector3Int min;
        public Vector3Int max;
        public readonly int noise;

        public StructureContextTreeCrown(int id, ref Vector3Int chunkPos, ref Vector3Int worldPos, ref Vector3Int min, ref Vector3Int max, int noise) : base(id, ref chunkPos)
        {
            this.worldPos = worldPos;
            this.min = min;
            this.max = max;
            this.noise = noise;
        }

        public override void Apply(Chunk chunk)
        {
            StructureTree.GenerateCrownPart(chunk, ref worldPos, ref min, ref max, noise);
        }
    }

    public class StructureContextTreeTrunk : StructureContext
    {
        public Vector3Int min;
        public Vector3Int max;

        public StructureContextTreeTrunk(int id, ref Vector3Int chunkPos, ref Vector3Int min, ref Vector3Int max) : base(id, ref chunkPos)
        {
            this.min = min;
            this.max = max;
        }

        public override void Apply(Chunk chunk)
        {
            StructureTree.GenerateTrunkPart(chunk, ref min, ref max);
        }
    }

    [Preserve]
    public class StructureTree : GeneratedStructure
    {
        private static BlockData leaves;
        private static BlockData log;

        private const int MIN_CROWN_SIZE = 3;
        private const int MIN_TRUNK_SIZE = 3;

        public override void Init(World world)
        {
            // Following variables are static. It's enough to set them up once
            if (leaves.Type == 0)
            {
                Block blk = world.blockProvider.GetBlock("leaves");
                leaves = new BlockData(blk.type, blk.solid);
                blk = world.blockProvider.GetBlock("log");
                log = new BlockData(blk.type, blk.solid);
            }
        }

        public override void Build(Chunk chunk, int id, ref Vector3Int worldPos, TerrainLayer layer)
        {
            World world = chunk.World;

            int noise =
                Helpers.FastFloor(NoiseUtils.GetNoise(layer.Noise.Noise, worldPos.x, worldPos.y, worldPos.z, 1f, 3, 1f));
            int leavesRange = MIN_CROWN_SIZE + noise;
            int leavesRange1 = leavesRange - 1;
            int trunkHeight = MIN_TRUNK_SIZE + noise;

            // Make the crown an ellipsoid flattened on the y axis
            float a2inv = 1.0f / (leavesRange * leavesRange);
            float b2inv = 1.0f / (leavesRange1 * leavesRange1);

            int x1 = worldPos.x - leavesRange;
            int x2 = worldPos.x + leavesRange;
            int y1 = worldPos.y + 1 + trunkHeight;
            int y2 = y1 + 1 + 2 * leavesRange1;
            int z1 = worldPos.z - leavesRange;
            int z2 = worldPos.z + leavesRange;

            int cx, cy, cz;
            int minX, minY, minZ;
            int maxX, maxY, maxZ;

            AABBInt bounds = new AABBInt(x1, worldPos.y, z1, x2, y2, z2);
            Vector3Int chunkPos = chunk.Pos;
            StructureInfo info = null;

            // Generate the crown
            Vector3Int posFrom = new Vector3Int(x1, y1, z1);
            Vector3Int posTo = new Vector3Int(x2, y2, z2);
            Vector3Int chunkPosFrom = Helpers.ContainingChunkPos(ref posFrom);
            Vector3Int chunkPosTo = Helpers.ContainingChunkPos(ref posTo);

            minY = Helpers.Mod(posFrom.y, Env.CHUNK_SIZE);
            for (cy = chunkPosFrom.y; cy <= chunkPosTo.y; cy += Env.CHUNK_SIZE, minY = 0)
            {
                maxY = System.Math.Min(posTo.y - cy, Env.CHUNK_SIZE_1);
                minZ = Helpers.Mod(posFrom.z, Env.CHUNK_SIZE);
                for (cz = chunkPosFrom.z; cz <= chunkPosTo.z; cz += Env.CHUNK_SIZE, minZ = 0)
                {
                    maxZ = System.Math.Min(posTo.z - cz, Env.CHUNK_SIZE_1);
                    minX = Helpers.Mod(posFrom.x, Env.CHUNK_SIZE);
                    for (cx = chunkPosFrom.x; cx <= chunkPosTo.x; cx += Env.CHUNK_SIZE, minX = 0)
                    {
                        maxX = System.Math.Min(posTo.x - cx, Env.CHUNK_SIZE_1);

                        int xOff = cx - worldPos.x;
                        int yOff = cy - y1 - leavesRange1;
                        int zOff = cz - worldPos.z;

                        if (cx != chunk.Pos.x || cy != chunk.Pos.y || cz != chunk.Pos.z)
                        {
                            Vector3Int pos = new Vector3Int(cx, cy, cz);
                            Vector3Int min = new Vector3Int(minX, minY, minZ);
                            Vector3Int max = new Vector3Int(maxX, maxY, maxZ);

                            if (info == null)
                            {
                                info = new StructureInfo(id, ref chunkPos, ref bounds);
                            }

                            world.RegisterPendingStructure(info, new StructureContextTreeCrown(id, ref pos, ref worldPos, ref min, ref max, noise));

                            continue;
                        }

                        // Actual crown construction
                        ChunkBlocks blocks = chunk.Blocks;
                        int index = Helpers.GetChunkIndex1DFrom3D(minX, minY, minZ);
                        int yOffset = Env.CHUNK_SIZE_WITH_PADDING_POW_2 - (maxZ - minZ + 1) * Env.CHUNK_SIZE_WITH_PADDING;
                        int zOffset = Env.CHUNK_SIZE_WITH_PADDING - (maxX - minX + 1);
                        for (int y = minY; y <= maxY; ++y, index += yOffset)
                        {
                            for (int z = minZ; z <= maxZ; ++z, index += zOffset)
                            {
                                for (int x = minX; x <= maxX; ++x, ++index)
                                {
                                    int xx = x + xOff;
                                    int yy = y + yOff;
                                    int zz = z + zOff;

                                    float _x = xx * xx * a2inv;
                                    float _y = yy * yy * b2inv;
                                    float _z = zz * zz * a2inv;
                                    if (_x + _y + _z <= 1.0f)
                                    {
                                        blocks.SetRaw(index, leaves);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // Generate the trunk
            posFrom = new Vector3Int(worldPos.x, worldPos.y, worldPos.z);
            posTo = new Vector3Int(worldPos.x, worldPos.y + trunkHeight, worldPos.z);
            chunkPosFrom = Helpers.ContainingChunkPos(ref posFrom);
            chunkPosTo = Helpers.ContainingChunkPos(ref posTo);

            cx = Helpers.MakeChunkCoordinate(worldPos.x);
            cz = Helpers.MakeChunkCoordinate(worldPos.z);

            int tx = Helpers.Mod(worldPos.x, Env.CHUNK_SIZE);
            int tz = Helpers.Mod(worldPos.z, Env.CHUNK_SIZE);

            minY = Helpers.Mod(posFrom.y, Env.CHUNK_SIZE);
            for (cy = chunkPosFrom.y; cy <= chunkPosTo.y; cy += Env.CHUNK_SIZE, minY = 0)
            {
                maxY = System.Math.Min(posTo.y - cy, Env.CHUNK_SIZE_1);

                if (cx != chunk.Pos.x || cy != chunk.Pos.y || cz != chunk.Pos.z)
                {
                    Vector3Int pos = new Vector3Int(cx, cy, cz);
                    Vector3Int min = new Vector3Int(tx, minY, tz);
                    Vector3Int max = new Vector3Int(tx, maxY, tz);

                    if (info == null)
                    {
                        info = new StructureInfo(id, ref chunkPos, ref bounds);
                    }

                    world.RegisterPendingStructure(info, new StructureContextTreeTrunk(id, ref pos, ref min, ref max));

                    continue;
                }

                // Actual trunk construction
                ChunkBlocks blocks = chunk.Blocks;
                int index = Helpers.GetChunkIndex1DFrom3D(tx, minY, tz);
                for (int y = minY; y <= maxY; ++y, index += Env.CHUNK_SIZE_WITH_PADDING_POW_2)
                {
                    blocks.SetRaw(index, log);
                }
            }
        }

        public static void GenerateCrownPart(Chunk chunk, ref Vector3Int worldPos, ref Vector3Int min, ref Vector3Int max,
            int noise)
        {
            ChunkBlocks blocks = chunk.Blocks;

            int leavesRange = MIN_CROWN_SIZE + noise;
            int leavesRange1 = leavesRange - 1;
            int trunkHeight = MIN_TRUNK_SIZE + noise;

            // Make the crown an ellipsoid flattened on the y axis
            float a2inv = 1.0f / (leavesRange * leavesRange);
            float b2inv = 1.0f / (leavesRange1 * leavesRange1);

            int y1 = worldPos.y + 1 + trunkHeight;
            int xOff = chunk.Pos.x - worldPos.x;
            int yOff = chunk.Pos.y - y1 - leavesRange1;
            int zOff = chunk.Pos.z - worldPos.z;

            blocks.Chunk.Modify(
                new ModifyOpEllipsoid(leaves, min, max, new Vector3Int(xOff, yOff, zOff), a2inv, b2inv, false)
                );
        }

        public static void GenerateTrunkPart(Chunk chunk, ref Vector3Int min, ref Vector3Int max)
        {
            ChunkBlocks blocks = chunk.Blocks;

            blocks.Chunk.Modify(new ModifyOpCuboid(log, min, max, false));
        }
    }
}
