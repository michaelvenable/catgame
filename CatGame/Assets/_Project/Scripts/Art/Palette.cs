using UnityEngine;

namespace CatGame
{
    // Art bible: Saturday-morning cartoon brawler — chunky rounded cats, soft toon bands,
    // sunny backyard world. Every color in the project comes from here.
    public static class Palette
    {
        public static readonly Color Ground = Hex("#7DB35A");        // meadow grass
        public static readonly Color Structure = Hex("#E6D3B0");     // sandstone
        public static readonly Color CatPrimary = Hex("#F08A3C");    // marmalade
        public static readonly Color CatSecondary = Hex("#FFF1DC");  // cream
        public static readonly Color Props = Hex("#6E8FB8");         // denim
        public static readonly Color Shadow = Hex("#4A4E6E");        // dusk indigo
        public static readonly Color Accent = Hex("#FFC93C");        // sunflower: hits, pickups

        public static readonly Color Sun = Hex("#FFEFD6");
        public static readonly Color SkyTint = Hex("#8FC1EE");
        public static readonly Color AmbientSky = Hex("#A9C8E8");
        public static readonly Color AmbientEquator = Hex("#B9B4A8");
        public static readonly Color AmbientGround = Hex("#5C4B3B");

        static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out Color c) ? c : Color.magenta;
    }
}
