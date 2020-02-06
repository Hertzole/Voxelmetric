﻿using UnityEngine;
using UnityEngine.Scripting;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Geometry.Batchers;
using Voxelmetric.Code.Load_Resources.Blocks;
using Vector3Int = Voxelmetric.Code.Data_types.Vector3Int;

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

    public override void BuildBlock(Chunk chunk, ref Vector3Int localPos, int materialID)
    {
        CustomMeshBlockConfig.CustomMeshBlockData data = MeshConfig.Data;
        Rect texture = data.textures != null
                           ? data.textures.GetTexture(chunk, ref localPos, Direction.down)
                           : new Rect();

        RenderGeometryBatcher batcher = chunk.RenderGeometryHandler.Batcher;

        if (data.uvs == null)
        {
            batcher.AddMeshData(materialID, data.tris, data.verts, data.colors, localPos);
        }
        else if (data.colors == null)
        {
            batcher.AddMeshData(materialID, data.tris, data.verts, data.uvs, ref texture, localPos);
        }
        else
        {
            batcher.AddMeshData(materialID, data.tris, data.verts, data.colors, data.uvs, ref texture, localPos);
        }
    }
}
