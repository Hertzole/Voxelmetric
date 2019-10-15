﻿using System.Collections.Generic;
using UnityEngine;

namespace Voxelmetric.Code.Load_Resources.Textures
{
    public class TextureProvider
    {
        WorldConfig config;
        //TextureConfig[] configs;
        BlockCollection blocks;

        //! Texture atlas
        public readonly Dictionary<string, TextureCollection> textures;
        //! Texture atlas
        public Texture2D atlas;

        private const string NO_TEXTURE_NAME = "Voxelmetric_No_Texture_Pick_Neutral";

        public static TextureProvider Create()
        {
            return new TextureProvider();
        }

        private TextureProvider()
        {
            textures = new Dictionary<string, TextureCollection>();
        }

        public void Init(BlockCollection blocks, WorldConfig config)
        {
            this.config = config;
            this.blocks = blocks;
            //configs = LoadAllTextures();
            LoadTextureIndex();
        }

        private void LoadTextureIndex()
        {
            List<Texture2D> individualTextures = blocks.GetAllUniqueTextures();
            Texture2D neutralTexture = Texture2D.whiteTexture;
            neutralTexture.name = NO_TEXTURE_NAME;

            //individualTextures.Add(neutralTexture);
            //for (int i = 0; i < configs.Length; i++)
            //{
            //    for (int j = 0; j < configs[i].textures.Length; j++)
            //    {
            //        //create an array of all these textures
            //        individualTextures.Add(configs[i].textures[j].texture2d);
            //    }
            //}

            // Generate atlas
            Texture2D packedTextures = new Texture2D(8192, 8192);
            Rect[] rects = packedTextures.PackTextures(individualTextures.ToArray(), config.textureAtlasPadding, 8192, false);

            // Transfer over the pixels to another texture2d because PackTextures resets the texture format and useMipMaps settings
            atlas = new Texture2D(packedTextures.width, packedTextures.height, config.textureFormat, config.useMipMaps);
            atlas.SetPixels(packedTextures.GetPixels(0, 0, packedTextures.width, packedTextures.height));
            atlas.filterMode = config.textureAtlasFiltering;

            List<Rect> repeatingTextures = new List<Rect>();
            List<Rect> nonrepeatingTextures = new List<Rect>();

            int index = 0;
            textures.Clear();
            //for (int i = 0; i < configs.Length; i++)
            //{
            //    for (int j = 0; j < configs[i].textures.Length; j++)
            //    {
            //        TextureConfig.Texture t = configs[i].textures[j];
            //        Rect uvs = rects[index];

            //        TextureCollection collection;
            //        if (!textures.TryGetValue(configs[i].name, out collection))
            //        {
            //            collection = new TextureCollection(configs[i].name, configs[i].type);
            //            textures.Add(configs[i].name, collection);
            //        }

            //        collection.AddTexture(uvs, t);

            //        if (configs[i].textures[j].repeating)
            //        {
            //            repeatingTextures.Add(rects[index]);
            //        }
            //        else
            //        {
            //            nonrepeatingTextures.Add(rects[index]);
            //        }

            //        index++;
            //    }
            //}



            for (int i = 0; i < individualTextures.Count; i++)
            {
                if (individualTextures[i] == null)
                {
                    individualTextures[i] = neutralTexture;
                }

                Rect uvs = rects[index];

                if (!textures.TryGetValue(individualTextures[i].name, out TextureCollection collection))
                {
                    collection = new TextureCollection(individualTextures[i].name, TextureConfigType.Simple);
                    textures.Add(individualTextures[i].name, collection);
                }

                collection.AddTexture(uvs, new TextureConfig.Texture() { weight = 1, index = 0 });

                nonrepeatingTextures.Add(rects[index]);

                index++;
            }

            //uPaddingBleed.BleedEdges(atlas, config.textureAtlasPadding, repeatingTextures.ToArray(), true);
            uPaddingBleed.BleedEdges(atlas, config.textureAtlasPadding, nonrepeatingTextures.ToArray(), false);
        }

        private TextureConfig[] LoadAllTextures()
        {
            //TextureConfig[] allConfigs = new ConfigLoader<TextureConfig>(new[] { config.textureFolder }).AllConfigs();
            List<Texture2D> textures = blocks.GetAllUniqueTextures();
            TextureConfig[] allConfigs = new TextureConfig[textures.Count];

            // Load all files in Textures folder
            //Texture2D[] sourceTextures = Resources.LoadAll<Texture2D>(config.textureFolder);

            Dictionary<string, Texture2D> sourceTexturesLookup = new Dictionary<string, Texture2D>();
            for (int i = 0; i < textures.Count; i++)
            {
                sourceTexturesLookup.Add(textures[i].name, textures[i]);
            }

            for (int i = 0; i < allConfigs.Length; i++)
            {
                //TextureConfig cfg = allConfigs[i];

                //for (int n = 0; n < cfg.textures.Length; n++)
                //{
                //    cfg.textures[n].texture2d = Texture2DFromConfig(cfg.textures[n], sourceTexturesLookup);
                //}

                //if (cfg.type == TextureConfigType.Connected)
                //{
                //    // Create all 48 possibilities from the 5 supplied textures
                //    Texture2D[] newTextures = ConnectedTextures.ConnectedTexturesFromBaseTextures(cfg.textures);
                //    TextureConfig.Texture[] connectedTextures = new TextureConfig.Texture[48];

                //    for (int x = 0; x < newTextures.Length; x++)
                //    {
                //        connectedTextures[x].index = x;
                //        connectedTextures[x].texture2d = newTextures[x];
                //    }

                //    cfg.textures = connectedTextures;
                //}

                //allConfigs[i].textures = new TextureConfig.Texture[1] { Texture2DFromConfig(allConfigs[i].textures[0], sourceTexturesLookup) };
                //allConfigs[i].textures = new TextureConfig.Texture[1] {new TextureConfig.Texture() { texture2d = } }
            }

            return allConfigs;
        }

        private Texture2D Texture2DFromConfig(TextureConfig.Texture texture, Dictionary<string, Texture2D> sourceTexturesLookup)
        {
            Texture2D file;
            if (!sourceTexturesLookup.TryGetValue(texture.file, out file))
            {
                Debug.LogError("Config referred to nonexistent file: " + texture.file);
                return null;
            }

            //No width or height means this texture is the whole file
            if (texture.width == 0 && texture.height == 0)
            {
                return file;
            }

            //If theres a width and a height fetch the pixels specified by the rect as a texture
            Texture2D newTexture = new Texture2D(texture.width, texture.height, config.textureFormat, file.mipmapCount < 1);
            newTexture.SetPixels(0, 0, texture.width, texture.height, file.GetPixels(texture.x, texture.y, texture.width, texture.height));
            return newTexture;
        }

        public TextureCollection GetTextureCollection(string textureName)
        {
            if (string.IsNullOrWhiteSpace(textureName))
            {
                textureName = NO_TEXTURE_NAME;
            }

            if (textures.Keys.Count == 0)
            {
                LoadTextureIndex();
            }

            TextureCollection collection;
            textures.TryGetValue(textureName, out collection);
            return collection;
        }

    }
}
