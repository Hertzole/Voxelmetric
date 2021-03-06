﻿using System.Text;
using UnityEngine;

namespace Voxelmetric
{
    /// <summary>
    ///     Global object pools for often used heap objects.
    /// </summary>
    public class GlobalPools
    {
        public readonly ObjectPool<Chunk> chunkPool =
            new ObjectPool<Chunk>(ch => new Chunk(), 1024, true);

        public readonly ObjectPool<Mesh> meshPool =
            new ObjectPool<Mesh>(m => new Mesh(), 128, true);

        #region "Work items"
        /*
         * These need to be used with caution. Items are popped on the main thread and pushed back
         * on a separate thread.
         */
        public readonly ObjectPool<TaskPoolItem<Chunk>> SMTaskPI =
            new ObjectPool<TaskPoolItem<Chunk>>(m => new TaskPoolItem<Chunk>(), 2048, false);

        public readonly ObjectPool<ThreadPoolItem<Chunk>> SMThreadPI =
            new ObjectPool<ThreadPoolItem<Chunk>>(m => new ThreadPoolItem<Chunk>(), 2048, false);

        #endregion

        public readonly ArrayPoolCollection<Vector2> vector2ArrayPool = new ArrayPoolCollection<Vector2>(64);

        public readonly ArrayPoolCollection<Vector3> vector3ArrayPool = new ArrayPoolCollection<Vector3>(64);

        public readonly ArrayPoolCollection<Vector4> vector4ArrayPool = new ArrayPoolCollection<Vector4>(64);

        public readonly ArrayPoolCollection<Color32> color32ArrayPool = new ArrayPoolCollection<Color32>(64);

        public readonly ArrayPoolCollection<byte> byteArrayPool = new ArrayPoolCollection<byte>(64);

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(256);
            sb.ConcatFormat("ChunkPool={0}", chunkPool.ToString());
            sb.ConcatFormat(",MeshPool={0}", meshPool.ToString());
            sb.ConcatFormat(",Vec2Arr={0}", vector2ArrayPool.ToString());
            sb.ConcatFormat(",Vec3Arr={0}", vector3ArrayPool.ToString());
            sb.ConcatFormat(",Vec4Arr={0}", vector4ArrayPool.ToString());
            sb.ConcatFormat(",ColorArr={0}", color32ArrayPool.ToString());
            sb.ConcatFormat(",ByteArr={0}", color32ArrayPool.ToString());
            return sb.ToString();
        }
    }
}
