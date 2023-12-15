using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FifMod.Utils;
using LethalLib.Modules;
using UnityEngine;

namespace FifMod
{
    public static class ContentManager
    {
        private static readonly Assembly _assembly = Assembly.GetExecutingAssembly();
        private static readonly Dictionary<Item, FifModObjectProperties> _objectProperties = new();

        public static bool TryGetObjectProperties(Item item, out FifModObjectProperties properties)
        {
            return _objectProperties.TryGetValue(item, out properties);
        }

        private static void RegisterObject(Item item, FifModObjectProperties properties)
        {
            _objectProperties.Add(item, properties);
            item.weight = FifModUtils.PoundsToItemWeight(properties.Weight);

            if (properties.CustomBehaviour != null)
            {
                var behaviour = (GrabbableObject)item.spawnPrefab.AddComponent(properties.CustomBehaviour);
                behaviour.itemProperties = item;
            }
            NetworkPrefabs.RegisterNetworkPrefab(item.spawnPrefab);
        }

        public static void RegisterContent(FifModAssets assets)
        {
            var itemsProperties = new List<FifModItemProperties>();
            var scrapsProperties = new List<FifModScrapProperties>();
            foreach (var type in _assembly.GetTypes())
            {
                if (type.IsAbstract) continue;

                if (type.IsSubclassOf(typeof(FifModItemProperties)))
                {
                    FifMod.Logger.LogInfo($"Found item properties: {type.Name}");
                    itemsProperties.Add((FifModItemProperties)Activator.CreateInstance(type));
                }
                else if (type.IsSubclassOf(typeof(FifModScrapProperties)))
                {
                    FifMod.Logger.LogInfo($"Found scrap properties: {type.Name}");
                    scrapsProperties.Add((FifModScrapProperties)Activator.CreateInstance(type));
                }
            }
            FifMod.Logger.LogInfo($"Loaded {itemsProperties.Count} items, {scrapsProperties.Count} scraps");

            var registeredItems = 0;
            foreach (var properties in itemsProperties)
            {
                if (!assets.TryGetAsset(properties.ItemAssetPath, out Item item))
                {
                    FifMod.Logger.LogWarning($"Item at path {properties.ItemAssetPath} was not found");
                    continue;
                }

                if (!assets.TryGetAsset(properties.InfoAssetPath, out TerminalNode info))
                {
                    FifMod.Logger.LogWarning($"Terminal Node at path {properties.InfoAssetPath} was not found");
                    continue;
                }

                FifMod.Logger.LogInfo($"Registering item | Name: {item.itemName} | Price: {properties.Price}");
                RegisterObject(item, properties);
                Items.RegisterShopItem(item, null, null, info, properties.Price);
                registeredItems++;
            }
            FifMod.Logger.LogInfo($"Registered {registeredItems} out of {itemsProperties.Count} items");

            var registeredScraps = 0;
            foreach (var properties in scrapsProperties)
            {
                if (!assets.TryGetAsset(properties.ItemAssetPath, out Item item))
                {
                    FifMod.Logger.LogWarning($"Item at path {properties.ItemAssetPath} was not found");
                    continue;
                }

                item.minValue = (int)(properties.MinValue / 0.4f);
                item.maxValue = (int)(properties.MaxValue / 0.4f);

                var avgCost = (item.minValue + item.maxValue) / 2;
                FifMod.Logger.LogInfo($"Registering scrap | Name: {item.itemName} | Avg Cost: {avgCost}");
                RegisterObject(item, properties);
                Items.RegisterScrap(item, properties.Rarity, properties.Moons);
                registeredScraps++;
            }
            FifMod.Logger.LogInfo($"Registered {registeredScraps} out of {scrapsProperties.Count} scraps");

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
    }
}