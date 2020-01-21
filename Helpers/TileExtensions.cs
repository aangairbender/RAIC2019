using AiCup2019.Model;

namespace AiCup2019.Helpers
{
    public static class TileExtensions
    {
        public static bool IsPassable(this Tile tile)
        {
            return tile != Tile.Wall;
        }

        public static bool IsBlocking(this Tile tile)
        {
            return tile == Tile.Wall;
        }

        public static bool IsStand(this Tile tile)
        {
            return tile != Tile.Empty && tile != Tile.JumpPad;
        }
    }
}