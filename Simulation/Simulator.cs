using System;
using System.Collections.Generic;
using System.Linq;
using AiCup2019.Model;

namespace AiCup2019.Simulation
{
    public class Simulator
    {
        private readonly int _width;
        private readonly int _height;
        private readonly Tile[][] _tiles;
        private readonly Properties _properties;

        private const int BulletLimit = 1000;
        private readonly Bullet[] _bullets;
        private int _bulletsCount;

        private const int MinesLimit = 1000;
        private readonly Mine[] _mines;
        private int _minesCount;

        private readonly LootBox[] _lootBoxes;
        private int _lootBoxesCount;

        private readonly Unit[] _units;
        private readonly Player[] _players;

        private int _currentTick;

        // for faster calculation
        private double _unitHalfWidth;
        private double _unitWidth;
        private double _unitHeight;

        private double _secondsPerUpdate;

        public Simulator(ref Game game)
        {
            _tiles = game.Level.Tiles;
            _width = _tiles.Length;
            _height = _tiles[0].Length;

            _properties = game.Properties;
            _currentTick = game.CurrentTick;

            _players = game.Players;
            _units = game.Units;

            _lootBoxes = game.LootBoxes;
            _lootBoxesCount = game.LootBoxes.Length;

            _bullets = new Bullet[BulletLimit];
            _bulletsCount = game.Bullets.Length;
            for (int i = 0; i < _bulletsCount; ++i)
                _bullets[i] = game.Bullets[i];

            _mines = new Mine[MinesLimit];
            _minesCount = game.Mines.Length;
            for (int i = 0; i < _minesCount; ++i)
                _mines[i] = game.Mines[i];

            _unitWidth = _properties.UnitSize.X;
            _unitHalfWidth = _unitWidth / 2;
            _unitHeight = _properties.UnitSize.Y;

            _secondsPerUpdate = 1d / (_properties.TicksPerSecond * _properties.UpdatesPerTick);
        }
        
        public void Tick(UnitAction[] actions)
        {
            for (int i = 0; i < _properties.UpdatesPerTick; ++i)
            {
                MicroTick(actions);
            }
        }

        private void MicroTick(UnitAction[] actions)
        {
            for (int unitIndex = 0; unitIndex < _units.Length; ++unitIndex)
            {
                UpdateUnit(ref _units[unitIndex], ref actions[unitIndex]);                
            }
        }

        private void UpdateUnit(ref Unit unit, ref UnitAction action)
        {
            double nx = unit.Position.X;
            double ny = unit.Position.Y;
            // we dont check bounds because map is wrapped by walls

            // horizontal movement
            nx += action.Velocity * _secondsPerUpdate;
            var tileYFrom = (int)ny;
            var tileYTo = (int)(ny + _unitHeight);
            var movedLeft = nx < unit.Position.X;
            var tileX = movedLeft ? (int) (nx - _unitHalfWidth) : (int) (nx + _unitHalfWidth);
            var collides = false;
            for (int tileY = tileYFrom; tileY <= tileYTo && !collides; ++tileY)
            {
                collides |= _tiles[tileX][tileY] == Tile.Wall;
            }
            if (collides)
                nx = movedLeft ? tileX + 1 + _unitHalfWidth : tileX - _unitHalfWidth;

            for (int i = 0; i < _units.Length; ++i)
            {
                var other = _units[i];
                if (!other.IsActive || other.Id == unit.Id)
                    continue;

                if (movedLeft && other.Position.X + _unitWidth > nx 
                              && other.Position.X + _unitWidth < unit.Position.X)
                    nx = other.Position.X + _unitWidth;
                else if (!movedLeft && nx + _unitWidth > other.Position.X
                                    && unit.Position.X + _unitWidth < other.Position.X)
                    nx = other.Position.X - _unitWidth;
            }

            // vertical movement
            var timeLeft = 1d;
            var falling = !action.Jump || unit.JumpState.Speed < 1e-3;
            var jumping = !falling;
            if (falling)
            {
                ny -= _properties.UnitFallSpeed * _secondsPerUpdate;
                var oldTileY = (int) unit.Position.Y;
                var tileY = (int) ny;
                var tileXFrom = (int) (nx - _unitHalfWidth);
                var tileXTo = (int) (nx + _unitHalfWidth);
                collides = false;
                for (tileX = tileXFrom; tileX <= tileXTo; ++tileX)
                {
                    collides |= _tiles[tileX][tileY] == Tile.Wall;
                    if (!action.JumpDown)
                    {
                        collides |= _tiles[tileX][tileY] == Tile.Ladder;
                        collides |= _tiles[tileX][tileY] == Tile.Platform && _tiles[tileX][oldTileY] != Tile.Platform;
                    }
                }

                var collisionY = -1d;
                if (collides)
                    collisionY = tileY + 1;

                for (int i = 0; i < _units.Length; ++i)
                {
                    var other = _units[i];
                    if (!other.IsActive || other.Id == unit.Id)
                        continue;

                    if (other.Position.Y + _unitHeight > ny && other.Position.Y + _unitHeight < unit.Position.Y)
                    {
                        collides = true;
                        collisionY = Math.Max(collisionY, other.Position.Y + _unitHeight);
                    }
                }

                if (collides)
                {
                    ny = collisionY;
                    if (action.Jump)
                    {
                        jumping = true;
                        //unit.JumpState.
                        timeLeft -= unit.Position.Y - collisionY;
                    }
                }
            }

            if (jumping)
            {
                ny += unit.JumpState.Speed * Math.Min(unit.JumpState.MaxTime * _secondsPerUpdate, timeLeft);
                var tileY = (int) (ny + _unitHeight);
                var tileXFrom = (int) (nx - _unitHalfWidth);
                var tileXTo = (int) (nx + _unitHalfWidth);
                collides = false;
                for (tileX = tileXFrom; tileX <= tileXTo; ++tileX)
                {
                    collides |= _tiles[tileX][tileY] == Tile.Wall;
                }

                var collisionY = 1000d;
                if (collides)
                    collisionY = tileY;

                for (int i = 0; i < _units.Length; ++i)
                {
                    var other = _units[i];
                    if (!other.IsActive || other.Id == unit.Id)
                        continue;

                    if (ny + _unitHeight > other.Position.Y && unit.Position.Y + _unitHeight < other.Position.Y)
                    {
                        collides = true;
                        collisionY = Math.Min(collisionY, other.Position.Y);
                    }
                }

                if (collides)
                {
                    ny = collisionY - _unitHeight;

                    timeLeft -= collisionY - (unit.Position.Y + _unitHeight);
                }
            }
        }
    }
}