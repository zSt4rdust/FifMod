using System;
using System.Collections.Generic;
using FifMod.Base;

namespace FifMod.Definitions
{
    public class SilverBarProperties : FifModScrapProperties
    {
        public override string ItemAssetPath => "Scraps/SilverBar/SilverBar.asset";
        public override FifModRarity Rarity => FifModRarity.All(25 * ConfigManager.ScrapsSilverBarRarity.Value);
        public override MoonFlags Moons => MoonFlags.All;

        public override Type CustomBehaviour => null;
        public override Dictionary<string, string> Tooltips => null;

        public override int Weight => 32;
        public override int MinValue => 75;
        public override int MaxValue => 140;
        public override ScrapSpawnFlags SpawnFlags => ScrapSpawnFlags.All;
    }
}