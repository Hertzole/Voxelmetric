﻿using UnityEngine;

namespace Voxelmetric
{
    public class CrossMeshBlockConfig : BlockConfig
    {
        public TextureCollection texture;
        public Color32 color;

        public override bool OnSetUp(BlockConfigObject config, World world)
        {
            if (!base.OnSetUp(config, world))
            {
                return false;
            }

            if (config is CrossMeshConfigObject crossMeshConfig)
            {
                texture = world.textureProvider.GetTextureCollection(crossMeshConfig.Texture.texture);
                color = crossMeshConfig.Texture.color;
            }
            else
            {
                Debug.LogError(config.GetType().Name + " config passed to cross mesh block.");
                return false;
            }

            return true;
        }
    }
}
