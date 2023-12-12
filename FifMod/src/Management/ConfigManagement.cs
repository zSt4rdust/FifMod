using BepInEx.Configuration;

namespace FifMod
{
    public static class ConfigManager
    {
        public static ConfigEntry<int> ItemsAxePrice { get; private set; }
        public static ConfigEntry<int> ItemsGlowstickPrice { get; private set; }

        public static ConfigEntry<int> ScrapsMagicBallRarity { get; private set; }
        public static ConfigEntry<int> ScrapsSilverBarRarity { get; private set; }

        public static void BindConfigFile(ConfigFile config)
        {
            ItemsAxePrice = config.Bind("Items", "Axe-Price", 110);
            ItemsGlowstickPrice = config.Bind("Items", "Glowstick-Price", 85);

            ScrapsMagicBallRarity = config.Bind("Scraps", "Magic-Ball-Rarity", 100);
            ScrapsSilverBarRarity = config.Bind("Scraps", "Silver-Bar-Rarity", 80);
        }
    }
}