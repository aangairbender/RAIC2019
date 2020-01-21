using System;
using System.Collections.Generic;
using System.Linq;
using AiCup2019.Helpers;
using AiCup2019.Model;
using AiCup2019.Navigation;
using AiCup2019.Shooting;

namespace AiCup2019
{
    public class MyStrategy
    {
        private PointMaster _pointMaster;
        private PathFinder _pathFinder;
        private RayTracer _rayTracer;

        private Unit _unit;
        private Game _game;
        private Debug _debug;

        public UnitAction GetAction(Unit unit, Game game, Debug debug)
        {
            _game = game;
            _unit = unit;
            _debug = debug;
            if (_pointMaster == null)
                _pointMaster = new PointMaster(ref game);
            if (_pathFinder == null)
                _pathFinder = new PathFinder(_pointMaster);
            if (_rayTracer == null)
                _rayTracer = new RayTracer(ref game);
            _pointMaster.DebugDraw(debug);

            var action = new UnitAction();

            var enemy = _game.Units.First(u => u.PlayerId != unit.PlayerId);

            if (!_unit.Weapon.HasValue || unit.Weapon.Value.Typ == WeaponType.RocketLauncher)
            {
                action.SwapWeapon = true;
                var lootbox = _game.LootBoxes
                    .Where(lb => lb.Item is Item.Weapon)
                    .Select(lb => (lb,
                        Math.Abs(lb.Position.X - unit.Position.X) + Math.Abs(lb.Position.Y - unit.Position.Y)))
                    .OrderBy(x => (x.Item1.Item as Item.Weapon).WeaponType == WeaponType.RocketLauncher ? x.Item2 * 100 : x.Item2 )
                    .First().Item1;
                if (GoToTarget(lootbox.Position, lootbox.Size.X / 2, ref action))
                    return action;
            }
            else
            {
                action.Aim = new Vec2Double(enemy.Position.X - _unit.Position.X, enemy.Position.Y - _unit.Position.Y);
                var hitProb = _rayTracer.PercentOfHit(ref _unit, Math.Atan2(action.Aim.Y, action.Aim.X), ref enemy);
                action.Shoot = hitProb > 0.5;
            }

            if (_unit.Health < _game.Properties.UnitMaxHealth && _game.LootBoxes.Count(x => x.Item is Item.HealthPack) > 0)
            {
                var hpkit = _game.LootBoxes
                    .Where(lb => lb.Item is Item.HealthPack)
                    .Select(lb => (lb,
                        Math.Abs(lb.Position.X - unit.Position.X) + Math.Abs(lb.Position.Y - unit.Position.Y)))
                    .OrderBy(x => x.Item2)
                    .First().Item1;
                if (GoToTarget(hpkit.Position, hpkit.Size.X / 2, ref action))
                    return action;
            }

            if (GoToTarget(enemy.Position, enemy.Size.X / 2, ref action))
                return action;

            return action;
        }

        private bool GoToTarget(Vec2Double pos, double dist, ref UnitAction action)
        {
            var meX = (int)(_unit.Position.X * PointMaster.Divisor + 0.5);
            var meY = (int)(_unit.Position.Y * PointMaster.Divisor + 0.1);
            
            var targetX = (int)(pos.X * PointMaster.Divisor + 0.5);
            var targetY = (int)(pos.Y * PointMaster.Divisor + 0.1);

            var jumping = !(_pointMaster.IsPointGround(meX, meY) || _pointMaster.IsPointLadder(meX, meY)) && _unit.JumpState.Speed > 0;
            var path = _pathFinder.FindPath(
                NavigationUtils.EncodeState(meX, meY,
                    jumping ? (int)(_unit.JumpState.MaxTime * 60 + 0.5) : 0,
                    jumping && !_unit.JumpState.CanCancel ? 1 : 0),
                targetX, targetY,
                (int)(dist * PointMaster.Divisor)
            );

            if (path == null || path.Count == 0)
                return false;

            /*for (int i = 0; i < path.Count; ++i)
            {
                var (x, y, j, p) = NavigationUtils.DecodeState(path[i]);
                if (i == 0)
                {
                    _debug.Draw(new CustomData.Line(_unit.Position.ToVec2Double(), new Vec2Float(1f * x / PointMaster.Divisor, 1f * y / PointMaster.Divisor), 0.075f,
                        new ColorFloat(1, 0, 1, 1)));
                }

                if (i + 1 == path.Count)
                {
                    _debug.Draw(new CustomData.Line(pos.ToVec2Double(), new Vec2Float(1f * x / PointMaster.Divisor, 1f * y / PointMaster.Divisor), 0.075f,
                        new ColorFloat(0, 1, 0, 1)));
                }
                else
                {
                    var (nx, ny, nj, np) = NavigationUtils.DecodeState(path[i + 1]);
                    _debug.Draw(new CustomData.Line(
                        new Vec2Float(1f * x / PointMaster.Divisor, 1f * y / PointMaster.Divisor),
                        new Vec2Float(1f * nx / PointMaster.Divisor, 1f * ny / PointMaster.Divisor), 0.05f,
                        new ColorFloat(1, 0, 0, 1)));
                }
            }

            _debug.Draw(new CustomData.Line(_unit.Position.ToVec2Double(), new Vec2Float(1f * meX / PointMaster.Divisor, 1f * meY / PointMaster.Divisor), 0.1f,
                new ColorFloat(0, 1, 1, 1)));*/

            var (x1, y1, j1, p1) = NavigationUtils.DecodeState(path[0]);
            action.Jump = y1 > meY || (y1 == meY && _unit.Position.Y < 1d * meY / PointMaster.Divisor);
            action.JumpDown = y1 < meY && (_pointMaster.IsPointDrop(meX, meY) || _unit.OnLadder ||
                                           (_game.Level.Tiles[(int) _unit.Position.X]
                                                [(int) (_unit.Position.Y - 1e-3)] == Tile.Ladder)); //hack
            if (j1 == 32 && !_unit.OnGround)
                action.Velocity = 0;
            else
                action.Velocity = (1d * x1 / PointMaster.Divisor - _unit.Position.X) * _game.Properties.TicksPerSecond;

            return true;
        }
    }
}