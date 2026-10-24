using System.Collections.Generic;
using GemPop.Items;

namespace GemPop.Data
{
    public enum ItemKind { Cube, RandomCube, Rocket, Bomb, Rock, Crate, ChestPart }

    public enum ChestCorner { BottomLeft, BottomRight, TopLeft, TopRight }

    /// <summary>Parsed meaning of one grid code such as "r", "rand", "vro", "cbTL".</summary>
    public readonly struct ItemSpec
    {
        public readonly ItemKind Kind;
        public readonly CubeColor Color;
        public readonly RocketDirection Direction;
        public readonly ChestCorner Corner;

        public ItemSpec(ItemKind kind, CubeColor color = default, RocketDirection direction = default, ChestCorner corner = default)
        {
            Kind = kind; Color = color; Direction = direction; Corner = corner;
        }
    }

    /// <summary>
    /// Translates the string codes used in level files into typed specs.
    /// The level format is only known here; the rest of the game never sees raw strings.
    /// </summary>
    public static class ItemCode
    {
        private static readonly Dictionary<string, ItemSpec> Table = new()
        {
            ["r"]    = new ItemSpec(ItemKind.Cube, CubeColor.Red),
            ["g"]    = new ItemSpec(ItemKind.Cube, CubeColor.Green),
            ["b"]    = new ItemSpec(ItemKind.Cube, CubeColor.Blue),
            ["y"]    = new ItemSpec(ItemKind.Cube, CubeColor.Yellow),
            ["rand"] = new ItemSpec(ItemKind.RandomCube),
            ["vro"]  = new ItemSpec(ItemKind.Rocket, direction: RocketDirection.Vertical),
            ["hro"]  = new ItemSpec(ItemKind.Rocket, direction: RocketDirection.Horizontal),
            ["t"]    = new ItemSpec(ItemKind.Bomb),
            ["s"]    = new ItemSpec(ItemKind.Rock),
            ["v"]    = new ItemSpec(ItemKind.Crate),
            ["cbBL"] = new ItemSpec(ItemKind.ChestPart, corner: ChestCorner.BottomLeft),
            ["cbBR"] = new ItemSpec(ItemKind.ChestPart, corner: ChestCorner.BottomRight),
            ["cbTL"] = new ItemSpec(ItemKind.ChestPart, corner: ChestCorner.TopLeft),
            ["cbTR"] = new ItemSpec(ItemKind.ChestPart, corner: ChestCorner.TopRight),
        };

        public static bool TryParse(string code, out ItemSpec spec) => Table.TryGetValue(code, out spec);
    }
}
