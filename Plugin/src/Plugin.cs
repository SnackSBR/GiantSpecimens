﻿using System.Reflection;
using UnityEngine;
using BepInEx;
using HarmonyLib;
using LethalLib.Modules;
using BepInEx.Logging;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System;
using GiantSpecimens.Dependency;
using GiantSpecimens.Configs;
using GiantSpecimens.Patches;
using GiantSpecimens.src;
using LethalLevelLoader;
using BepInEx.Bootstrap;
using GiantSpecimens.Scrap;
using MoreShipUpgrades.Misc;
using Unity.Netcode;
using MoreShipUpgrades.Managers;

namespace GiantSpecimens;
[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
[BepInDependency("BMX.LobbyCompatibility", Flags:BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("MegaPiggy.EnumUtils", Flags:BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency(LethalLevelLoader.Plugin.ModGUID)]
public class Plugin : BaseUnityPlugin {
    public static Harmony _harmony = new(PluginInfo.PLUGIN_GUID);
    public static ExtendedEnemyType PinkGiant;
    public static ExtendedEnemyType DriftGiant;
    public static ExtendedItem RedWoodPlushie;
    public static ExtendedItem Whistle;
    public static GiantSpecimensConfig ModConfig { get; private set; } // prevent from accidently overriding the config
    internal static new ManualLogSource Logger;
    public static CauseOfDeath RupturedEardrums = EnumUtils.Create<CauseOfDeath>("RupturedEardrums");
    public static CauseOfDeath InternalBleed = EnumUtils.Create<CauseOfDeath>("InternalBleed");
    public static CauseOfDeath Thwomped = EnumUtils.Create<CauseOfDeath>("Thwomped");
    public static ThreatType DriftwoodGiant = EnumUtils.Create<ThreatType>("DriftwoodGiant");
    public static Dictionary<string, Item> samplePrefabs = [];
    public static bool LGULoaded;
    BepInEx.PluginInfo LGU;
    private void Awake() {
        Logger = base.Logger;
        // Lobby Compatibility stuff
        if (LobbyCompatibilityChecker.Enabled) {
            LobbyCompatibilityChecker.Init();
        }

        ModConfig = new GiantSpecimensConfig(this.Config); // Create the config with the file from here.

        AssetBundleLoader.AddOnExtendedModLoadedListener(OnExtendedModRegistered, "XuXiaolan");
        AssetBundleLoader.AddOnLethalBundleLoadedListener(OnLethalBundleLoaded, "giantspecimenassets.lethalbundle");

        /*if (Chainloader.PluginInfos.TryGetValue(Metadata.GUID, out LGU)) {
                LGULoaded = true;
                Logger.LogInfo($"MNC = {LGU}");
                RegisterLGUSample(DriftwoodSample, "DriftWoodGiant", 2);
                RegisterLGUSample(RedWoodHeart, "RedWoodGiant", 3);
                Destroy(RedWoodHeart.spawnPrefab.GetComponent<RedwoodHeart>());
                Destroy(DriftwoodSample.spawnPrefab.GetComponent<DriftwoodHeart>());
        } else {
            LGULoaded = false;
            RegisterScrap(DriftwoodSample, 0, LevelTypes.All);
            RegisterScrap(RedWoodHeart, 0, LevelTypes.All);*/
        //}
        GiantPatches.Init();

        Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

        // Required by https://github.com/EvaisaDev/UnityNetcodePatcher
        InitializeNetworkBehaviours();
        _harmony.PatchAll(typeof(StartOfRoundPatcher));
    }
    internal static void OnExtendedModRegistered(ExtendedMod extendedMod) {
        if (extendedMod == null) return;
        foreach (ExtendedEnemyType extendedEnemyType in extendedMod.ExtendedEnemyTypes) {
            List<StringWithRarity> planetNames = new List<StringWithRarity>();
            if (extendedEnemyType.name == "RedwoodExtendedEnemyType") {
                planetNames = ConfigParsing(GiantSpecimensConfig.ConfigRedWoodRarity.Value);
                extendedEnemyType.EnemyType.PowerLevel = GiantSpecimensConfig.ConfigRedwoodGiantPower.Value;
            } else if (extendedEnemyType.name == "DriftwoodExtendedEnemyType") {
                planetNames = ConfigParsing(GiantSpecimensConfig.ConfigDriftWoodRarity.Value);
                extendedEnemyType.EnemyType.PowerLevel = GiantSpecimensConfig.ConfigDriftwoodGiantPower.Value;
            }
            extendedEnemyType.OutsideLevelMatchingProperties.planetNames.AddRange(planetNames);
            Plugin.Logger.LogInfo($"Configured {extendedEnemyType.name} with new planet names and rarities.");
        }
        foreach (ExtendedItem extendedItem in extendedMod.ExtendedItems) {
            List<StringWithRarity> planetNames = new List<StringWithRarity>();
            if (extendedItem.name == "RedwoodPlushieExtendedItem") {
                planetNames = ConfigParsing(GiantSpecimensConfig.ConfigRedwoodPlushieRarity.Value);

            } else if (extendedItem.name == "DriftwoodPlushieExtendedItem") {
                planetNames = ConfigParsing(GiantSpecimensConfig.ConfigDriftWoodPlushieRarity.Value);

            } else if (extendedItem.name == "WhistleExtendedItem") {
                extendedItem.IsBuyableItem = GiantSpecimensConfig.ConfigWhistleEnabled.Value;
                planetNames = ConfigParsing(GiantSpecimensConfig.ConfigDriftWoodPlushieRarity.Value);

            } else if (extendedItem.name == "DriftwoodSampleExtendedItem") {
                samplePrefabs.Add("DriftWoodGiant", extendedItem.Item);
                extendedItem.LevelMatchingProperties.planetNames.AddRange(ConfigParsing("Vanilla:0,Custom:0")); // I don't think LLL does this for whatever reason

            } else if (extendedItem.name == "RedwoodHeartExtendedItem") {
                samplePrefabs.Add("RedWoodGiant", extendedItem.Item);
                extendedItem.LevelMatchingProperties.planetNames.AddRange(ConfigParsing("Vanilla:0,Custom:0")); // I don't think LLL does this for whatever reason

            }
            extendedItem.LevelMatchingProperties.planetNames.AddRange(planetNames);
            Plugin.Logger.LogInfo($"Configured {extendedItem.name} with new planet names and rarities.");
        }
    }
    internal static void OnLethalBundleLoaded(AssetBundle assetBundle) {
            if (assetBundle == null) return;
    }
    private static List<StringWithRarity> ConfigParsing(string configMoonRarity) {
        List<StringWithRarity> spawnRates = new List<StringWithRarity>();

        foreach (string entry in configMoonRarity.Split(ConfigHelper.indexSeperator).Select(s => s.Trim())) {
            string[] entryParts = entry.Split(ConfigHelper.keyPairSeperator);
            if (entryParts.Length != 2) {
                continue;
            }

            string name = entryParts[0];
            if (int.TryParse(entryParts[1], out int spawnrate)) {
                spawnRates.Add(new StringWithRarity(name, spawnrate));
                Plugin.Logger.LogInfo($"Registered spawn rate for {name} to {spawnrate}");
            }
        }
        return spawnRates;
    }

    private void InitializeNetworkBehaviours() {
        var types = Assembly.GetExecutingAssembly().GetTypes();
        foreach (var type in types)
        {
            var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            foreach (var method in methods)
            {
                var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                if (attributes.Length > 0)
                {
                    method.Invoke(null, null);
                }
            }
        }
    }
    private void RegisterLGUSample(Item sample, string monster, int level) {
        if (LGULoaded) {
            var type = typeof(MoreShipUpgrades.API.HunterSamples);
            MethodInfo info = type.GetMethod("RegisterSample", [typeof(Item), typeof(string), typeof(int), typeof(bool), typeof(bool)]);
            info!.Invoke(null, [sample, monster, level, false, true]);
        }
    }
}