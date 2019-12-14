﻿namespace SMLHelper.V2.Patchers
{
    using Harmony;
    using System.Collections.Generic;
    using Logger = V2.Logger;

    internal class LootDistributionPatcher
    {
        internal static readonly SelfCheckingDictionary<string, LootDistributionData.SrcData> CustomSrcData = new SelfCheckingDictionary<string, LootDistributionData.SrcData>("CustomSrcData");

        internal static void Patch(HarmonyInstance harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(LootDistributionData), "Initialize"),
                postfix: new HarmonyMethod(AccessTools.Method(typeof(LootDistributionPatcher), "InitializePostfix")));

            Logger.Log("LootDistributionPatcher is done.", LogLevel.Debug);
        }

        private static void InitializePostfix(LootDistributionData __instance)
        {
            foreach(var entry in CustomSrcData)
            {
                LootDistributionData.SrcData customSrcData = entry.Value;
                string classId = entry.Key;

                if (__instance.srcDistribution.TryGetValue(entry.Key, out LootDistributionData.SrcData srcData))
                {
                    EditExistingData(classId, srcData, customSrcData, __instance.dstDistribution);
                }
                else
                {
                    AddCustomData(classId, customSrcData, __instance.srcDistribution, __instance.dstDistribution);
                }
            }
        }

        private static void EditExistingData(string classId, LootDistributionData.SrcData existingData, LootDistributionData.SrcData changes, Dictionary<BiomeType, LootDistributionData.DstData> dstData)
        {
            foreach (var customBiomeDist in changes.distribution)
            {
                bool foundBiome = false;

                for (int i = 0; i < existingData.distribution.Count; i++)
                {
                    LootDistributionData.BiomeData biomeDist = existingData.distribution[i];

                    if (customBiomeDist.biome == biomeDist.biome)
                    {
                        biomeDist.count = customBiomeDist.count;
                        biomeDist.probability = customBiomeDist.probability;

                        foundBiome = true;
                    }
                }

                if (!foundBiome)
                {
                    existingData.distribution.Add(customBiomeDist);
                }

                if (!dstData.TryGetValue(customBiomeDist.biome, out LootDistributionData.DstData biomeDistData))
                {
                    biomeDistData = new LootDistributionData.DstData();
                    biomeDistData.prefabs = new List<LootDistributionData.PrefabData>();
                    dstData.Add(customBiomeDist.biome, biomeDistData);
                }

                bool foundPrefab = false;

                for (int j = 0; j < biomeDistData.prefabs.Count; j++)
                {
                    LootDistributionData.PrefabData prefabData = biomeDistData.prefabs[j];

                    if (prefabData.classId == classId)
                    {
                        prefabData.count = customBiomeDist.count;
                        prefabData.probability = customBiomeDist.probability;

                        foundPrefab = true;
                    }
                }

                if (!foundPrefab)
                {
                    biomeDistData.prefabs.Add(new LootDistributionData.PrefabData()
                    {
                        classId = classId,
                        count = customBiomeDist.count,
                        probability = customBiomeDist.probability
                    });
                }
            }
        }

        private static void AddCustomData(string classId, LootDistributionData.SrcData customSrcData, Dictionary<string, LootDistributionData.SrcData> srcDistribution, Dictionary<BiomeType, LootDistributionData.DstData> dstDistribution)
        {
            srcDistribution.Add(classId, customSrcData);

            List<LootDistributionData.BiomeData> distribution = customSrcData.distribution;

            if (distribution != null)
            {
                for (int i = 0; i < distribution.Count; i++)
                {
                    LootDistributionData.BiomeData biomeData = distribution[i];
                    BiomeType biome = biomeData.biome;
                    int count = biomeData.count;
                    float probability = biomeData.probability;
                    LootDistributionData.DstData dstData;

                    if (!dstDistribution.TryGetValue(biome, out dstData))
                    {
                        dstData = new LootDistributionData.DstData();
                        dstData.prefabs = new List<LootDistributionData.PrefabData>();
                        dstDistribution.Add(biome, dstData);
                    }

                    LootDistributionData.PrefabData prefabData = new LootDistributionData.PrefabData();
                    prefabData.classId = classId;
                    prefabData.count = count;
                    prefabData.probability = probability;
                    dstData.prefabs.Add(prefabData);
                }
            }
        }
    }
}