﻿using System.Collections.Generic;
using UnityEngine;

namespace Voxelmetric
{
    public sealed class ChunkLogic
    {
        private readonly Chunk chunk;
        private float randomUpdateTime;
        private readonly List<BlockAndTimer> scheduledUpdates = new List<BlockAndTimer>();

        public ChunkLogic(Chunk chunk)
        {
            this.chunk = chunk;
            Reset();
        }

        public void Reset()
        {
            randomUpdateTime = 0;
            scheduledUpdates.Clear();
        }

        public void Update()
        {
            randomUpdateTime += Time.deltaTime;
            if (randomUpdateTime >= chunk.World.config.RandomUpdateFrequency)
            {
                randomUpdateTime = 0;

                Vector3Int randomVector3Int = new Vector3Int(
                    Voxelmetric.resources.random.Next(0, Env.CHUNK_SIZE),
                    Voxelmetric.resources.random.Next(0, Env.CHUNK_SIZE),
                    Voxelmetric.resources.random.Next(0, Env.CHUNK_SIZE)
                    );

                chunk.Blocks.GetBlock(ref randomVector3Int).RandomUpdate(chunk, ref randomVector3Int);

                // Process Scheduled Updates
                for (int i = 0; i < scheduledUpdates.Count; i++)
                {
                    scheduledUpdates[i] = new BlockAndTimer(
                        scheduledUpdates[i].pos,
                        scheduledUpdates[i].time - chunk.World.config.RandomUpdateFrequency
                        );

                    if (scheduledUpdates[i].time <= 0)
                    {
                        Vector3Int pos = scheduledUpdates[i].pos;
                        Block block = chunk.Blocks.GetBlock(ref pos);
                        block.ScheduledUpdate(chunk, ref pos);
                        scheduledUpdates.RemoveAt(i);
                        i--;
                    }
                }
            }

        }

        public void AddScheduledUpdate(Vector3Int vector3Int, float time)
        {
            scheduledUpdates.Add(new BlockAndTimer(vector3Int, time));
        }
    }
}
