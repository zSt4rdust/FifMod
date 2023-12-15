using System;
using System.Collections.Generic;
using LethalLib.Modules;

namespace FifMod
{
    public abstract class FifModProperties { }
    public abstract class FifModObjectProperties : FifModProperties
    {
        public abstract Type CustomBehaviour { get; }
        public abstract string ItemAssetPath { get; }
        public abstract Dictionary<string, string> Tooltips { get; }
        public abstract int Weight { get; }
    }

    public abstract class FifModScrapProperties : FifModObjectProperties
    {
        public abstract int Rarity { get; }
        public abstract Levels.LevelTypes Moons { get; }
        public abstract int MinValue { get; }
        public abstract int MaxValue { get; }
    }

    public abstract class FifModItemProperties : FifModObjectProperties
    {
        public abstract string InfoAssetPath { get; }
        public abstract int Price { get; }
    }
}