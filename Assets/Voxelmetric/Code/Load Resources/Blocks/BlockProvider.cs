﻿using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using Voxelmetric.Code.Core;

namespace Voxelmetric.Code.Load_Resources.Blocks
{
    public class BlockProvider
    {
        // Air type block will always be present
        public static readonly ushort AirType = 0;

        private Block[] m_blockTypes;
        private readonly List<BlockConfig> m_configs;
        private readonly Dictionary<string, ushort> m_names;

        public Block[] BlockTypes { get { return m_blockTypes;} }

        public static BlockProvider Create(string blockFolder, World world)
        {
            BlockProvider provider = new BlockProvider();
            provider.Init(blockFolder, world);
            return provider;
        }

        private BlockProvider()
        {
            m_names = new Dictionary<string, ushort>();
            m_configs = new List<BlockConfig>();
        }

        private void Init(string blockFolder, World world)
        {
            // Add the static air block type
            AddBlockType(new BlockConfig
            {
                name = "air",
                className = "Block",
                world = world,
                solid = false,
                canBeWalkedOn = false,
                transparent = true,
                canBeWalkedThrough = true,
            });
            
            // Add all the block definitions defined in the config files
            ProcessConfigs(world, blockFolder);

            // Build block type lookup table
            m_blockTypes = new Block[m_configs.Count];
            for (int i = 0; i< m_configs.Count; i++)
            {
                BlockConfig config = m_configs[i];

                Block block = (Block)Activator.CreateInstance(config.blockClass);
                block.type = (ushort)i;
                block.config = config;
                m_blockTypes[i] = block;
            }

            // Once all blocks are set up, call OnInit on them. It is necessary to do it in a separate loop
            // in order to ensure there will be no dependency issues.
            for (int i = 0; i < m_blockTypes.Length; i++)
            {
                Block block = m_blockTypes[i];
                block.OnInit(this);
            }
        }

        // World is only needed for setting up the textures
        private void ProcessConfigs(World world, string blockFolder)
        {
            var configFiles = Resources.LoadAll<TextAsset>(blockFolder);
            foreach (var configFile in configFiles)
            {
                Hashtable configHash = JsonConvert.DeserializeObject<Hashtable>(configFile.text);

                Type configType = Type.GetType(configHash["configClass"] + ", " + typeof(BlockConfig).Assembly, false);
                if (configType == null)
                {
                    Debug.LogError("Could not create config for " + configHash["configClass"]);
                    continue;
                }

                BlockConfig config = (BlockConfig)Activator.CreateInstance(configType);
                config.SetUp(configHash, world);

                if (!VerifyBlockConfig(config))
                    continue;

                AddBlockType(config);
            }
        }
        
        private bool VerifyBlockConfig(BlockConfig config)
        {
            // Unique identifier of block type
            if (m_names.ContainsKey(config.name))
            {
                Debug.LogErrorFormat("Two blocks with the name {0} are defined", config.name);
                return false;
            }

            // Class name must be valid
            if (config.blockClass == null)
            {
                Debug.LogErrorFormat("Invalid class name {0} for block {1}", config.className, config.name);
                return false;
            }

            // Use the type defined in the config if there is one, otherwise add one to the largest index so far
            if (config.type == ushort.MaxValue)
            {
                Debug.LogError("Maximum number of block types reached for " + config.name);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Adds a block type to the index and adds it's name to a dictionary for quick lookup
        /// </summary>
        /// <param name="config">The controller object for this block</param>
        /// <returns>The index of the block</returns>
        private void AddBlockType(BlockConfig config)
        {
            config.type = m_configs.Count;
            m_configs.Add(config);
            m_names.Add(config.name, (ushort)config.type);
        }

        public ushort GetType(string name)
        {
            ushort type;
            if (m_names.TryGetValue(name, out type))
                return type;

            Debug.LogError("Block not found: " + name);
            return AirType;
        }

        public Block GetBlock(string name)
        {
            ushort type;
            if (m_names.TryGetValue(name, out type))
                return BlockTypes[type];

            Debug.LogError("Block not found: " + name);
            return BlockTypes[AirType];
        }

        public BlockConfig GetConfig(ushort type)
        {
            if (type<m_configs.Count)
                return m_configs[type];

            Debug.LogError("Config not found: "+type);
            return m_configs[AirType];
        }
    }
}
