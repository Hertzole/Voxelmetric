﻿namespace Voxelmetric
{
    public class AbsoluteLayer : TerrainLayer
    {
        private BlockData blockToPlace;
        public int MinHeight { get; set; }
        public int MaxHeight { get; set; }
        private int amplitude;

        public float Frequency { get; set; }
        public float Exponent { get; set; }

        protected override void SetUp(LayerConfigObject config)
        {
            // Config files for absolute layers MUST define these properties
            Block block = world.blockProvider.GetBlock(config.BlockName);
            blockToPlace = new BlockData(block.type, block.solid);

            noise.Frequency = 1f / Frequency; // Frequency in configs is in fast 1/frequency
            noise.Gain = Exponent;
#if (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN) && ENABLE_FASTSIMD
            noiseSIMD.Frequency = noise.Frequency;
            noiseSIMD.Gain = noise.Gain;
#endif

            amplitude = MaxHeight - MinHeight;
        }

        public override void PreProcess(Chunk chunk, int layerIndex)
        {
            LocalPools pools = Globals.WorkPool.GetPool(chunk.ThreadID);
            NoiseItem ni = pools.noiseItems[layerIndex];
            ni.noiseGen.SetInterpBitStep(Env.CHUNK_SIZE_WITH_PADDING, 2);
            ni.lookupTable = pools.floatArrayPool.Pop((ni.noiseGen.Size + 1) * (ni.noiseGen.Size + 1));

#if (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN) && ENABLE_FASTSIMD
            float[] noiseSet = pools.floatArrayPool.Pop(ni.noiseGen.Size * ni.noiseGen.Size * ni.noiseGen.Size);

            // Generate SIMD noise
            int offsetShift = Env.CHUNK_POW - ni.noiseGen.Step;
            int xStart = (chunk.Pos.x * Env.CHUNK_SIZE) << offsetShift;
            int yStart = (chunk.Pos.y * Env.CHUNK_SIZE) << offsetShift;
            int zStart = (chunk.Pos.z * Env.CHUNK_SIZE) << offsetShift;
            float scaleModifier = 1 << ni.noiseGen.Step;
            noiseSIMD.Noise.FillNoiseSet(noiseSet, xStart, yStart, zStart, ni.noiseGen.Size, ni.noiseGen.Size, ni.noiseGen.Size, scaleModifier);

            // Generate a lookup table
            int i = 0;
            for (int z = 0; z < ni.noiseGen.Size; z++)
            {
                for (int x = 0; x < ni.noiseGen.Size; x++)
                {
                    ni.lookupTable[i++] = NoiseUtilsSIMD.GetNoise(noiseSet, ni.noiseGen.Size, x, 0, z, amplitude, noise.Gain);
                }
            }

            pools.floatArrayPool.Push(noiseSet);
#else
            int xOffset = chunk.Pos.x;
            int zOffset = chunk.Pos.z;

            // Generate a lookup table
            int i = 0;
            for (int z = 0; z < ni.noiseGen.Size; z++)
            {
                float zf = (z << ni.noiseGen.Step) + zOffset;

                for (int x = 0; x < ni.noiseGen.Size; x++)
                {
                    float xf = (x << ni.noiseGen.Step) + xOffset;
                    ni.lookupTable[i++] = NoiseUtils.GetNoise(noise.Noise, xf, 0, zf, 1f, amplitude, noise.Gain);
                }
            }
#endif
        }

        public override void PostProcess(Chunk chunk, int layerIndex)
        {
            LocalPools pools = Globals.WorkPool.GetPool(chunk.ThreadID);
            NoiseItem ni = pools.noiseItems[layerIndex];
            pools.floatArrayPool.Push(ni.lookupTable);
        }

        public override float GetHeight(Chunk chunk, int layerIndex, int x, int z, float heightSoFar, float strength)
        {
            LocalPools pools = Globals.WorkPool.GetPool(chunk.ThreadID);
            NoiseItem ni = pools.noiseItems[layerIndex];

            // Calculate height to add and sum it with the min height (because the height of this
            // layer should fluctuate between minHeight and minHeight+the max noise) and multiply
            // it by strength so that a fraction of the result that gets used can be decided
            float heightToAdd = ni.noiseGen.Interpolate(x, z, ni.lookupTable);
            heightToAdd += MinHeight;
            heightToAdd *= strength;

            // Absolute layers add from the minY and up but if the layer height is lower than
            // the existing terrain there's nothing to add so just return the initial value
            if (heightToAdd > heightSoFar)
            {
                //Return the height of this layer from minY as this is the new height of the column
                return heightToAdd;
            }

            return heightSoFar;
        }

        public override float GenerateLayer(Chunk chunk, int layerIndex, int x, int z, float heightSoFar, float strength)
        {
            LocalPools pools = Globals.WorkPool.GetPool(chunk.ThreadID);
            NoiseItem ni = pools.noiseItems[layerIndex];

            // Calculate height to add and sum it with the min height (because the height of this
            // layer should fluctuate between minHeight and minHeight+the max noise) and multiply
            // it by strength so that a fraction of the result that gets used can be decided
            float heightToAdd = ni.noiseGen.Interpolate(x, z, ni.lookupTable);
            heightToAdd += MinHeight;
            heightToAdd *= strength;

            // Absolute layers add from the minY and up but if the layer height is lower than
            // the existing terrain there's nothing to add so just return the initial value
            if (heightToAdd > heightSoFar)
            {
                SetBlocks(chunk, x, z, (int)heightSoFar, (int)heightToAdd, blockToPlace);

                //Return the height of this layer from minY as this is the new height of the column
                return heightToAdd;
            }

            return heightSoFar;
        }
    }
}
