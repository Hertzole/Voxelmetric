﻿using UnityEngine.Scripting;

namespace Voxelmetric
{
    public class CustomMeshBlock : Block
    {
        [Preserve]
        public CustomMeshBlock() : base() { }

        public CustomMeshBlockConfig MeshConfig
        {
            get { return (CustomMeshBlockConfig)config; }
        }

        public override void OnInit(BlockProvider blockProvider)
        {
            base.OnInit(blockProvider);

            custom = true;
        }

        //TODO: Build CustomMeshBlock
        public override void BuildBlock(Chunk chunk, ref Vector3Int localPos, int materialID)
        {
            //CustomMeshBlockConfig.CustomMeshBlockData data = MeshConfig.Data;
            ////Rect texture = data.textures != null ? data.textures.GetTexture(chunk, ref localPos, Direction.down) : new Rect();
            //Vector2 texture = data.textures != null ? data.textures.GetTexture() : Vector2.zero;

            //RenderGeometryBatcher batcher = chunk.RenderGeometryHandler.Batcher;

            //if (data.uvs == null)
            //{
            //    batcher.AddMeshData(materialID, data.tris, data.verts, data.colors, localPos);
            //}
            //else if (data.colors == null)
            //{
            //    batcher.AddMeshData(materialID, data.tris, data.verts, data.uvs, localPos);
            //}
            //else
            //{
            //    batcher.AddMeshData(materialID, data.tris, data.verts, data.colors, data.uvs, ref texture, localPos);
            //}
        }
    }
}
