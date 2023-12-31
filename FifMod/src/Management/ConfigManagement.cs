using BepInEx.Configuration;

namespace FifMod
{
    public static class ConfigManager
    {
        public static ConfigEntry<int> ItemsAxePrice { get; private set; }
        public static ConfigEntry<int> ItemsGlowstickPrice { get; private set; }

        public static ConfigEntry<float> ScrapsMagicBallRarity { get; private set; }
        public static ConfigEntry<float> ScrapsSilverBarRarity { get; private set; }

        public static ConfigEntry<int> MiscShipCapacity { get; private set; }

        public static void BindConfigFile(ConfigFile config)
        {
            ItemsAxePrice = config.Bind("Items", "Axe-Price", 110);
            ItemsGlowstickPrice = config.Bind("Items", "Glowstick-Price", 85);

            ScrapsMagicBallRarity = config.Bind("Scraps", "Magic-Ball-Rarity-Multiplier", 1f);
            ScrapsSilverBarRarity = config.Bind("Scraps", "Silver-Bar-Rarity-Multiplier", 1f);

            MiscShipCapacity = config.Bind("Misc", "Ship-Capacity", 999, "Increases maximum amount of items that game can save");
        }
    }
}