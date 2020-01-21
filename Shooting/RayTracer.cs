using System;
using AiCup2019.Model;

namespace AiCup2019.Shooting
{
    public class RayTracer
    {
        private readonly Game _game;
        private readonly Tile[][] _tiles;

        public RayTracer(ref Game game)
        {
            _game = game;
            _tiles = _game.Level.Tiles;
        }

        public bool IsHit(double sx, double sy, double angle, double size, ref Unit enemy)
        {
            var xDist = enemy.Size.X - 0.1;
            var yDist = 1 - 0.1; // tile is less than height

            var dx = Math.Cos(angle) * xDist;
            var dy = Math.Sin(angle) * yDist;

            var l = enemy.Position.X - enemy.Size.X / 2 - size / 2;
            var r = enemy.Position.X + enemy.Size.X / 2 + size / 2;
            var b = enemy.Position.Y - size / 2;
            var t = enemy.Position.Y + enemy.Size.Y + size / 2;

            for (int i = 0; i < 100; ++i)
            {
                sx += dx;
                sy += dy;

                if (sx > l && sx < r && sy > b && sy < t)
                    return true;

                if (_tiles[(int)sx][(int)sy] == Tile.Wall)
                    return false;
            }

            return false;
        }

        public double PercentOfHit(double sx, double sy, double aimAngle, double spread, double size, ref Unit enemy)
        {
            const int cnt = 100;
            var hits = 0;
            var cur = aimAngle - spread;
            var step = spread * 2 / cnt;
            for (int i = 0; i <= cnt; ++i)
            {
                hits += IsHit(sx, sy, cur, size, ref enemy) ? 1 : 0;
                cur += step;
            }

            return 1d * hits / (cnt + 1);
        }

        public double PercentOfHit(ref Unit me, double aimAngle, ref Unit enemy)
        {
            if (!me.Weapon.HasValue)
                return 0;

            return PercentOfHit(me.Position.X, me.Position.Y + me.Size.Y / 2, aimAngle,
                me.Weapon.Value.Spread, me.Weapon.Value.Parameters.Bullet.Size, ref enemy);
        }
    }
}