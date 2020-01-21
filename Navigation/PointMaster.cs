using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using AiCup2019.Helpers;
using AiCup2019.Model;

namespace AiCup2019.Navigation
{
    public class PointMaster
    {
        public const int Divisor = 6;
        public const int UnitHalfWidth = 2;
        public const int UnitHeight = 10;
        public const int UnitHalfHeight = 5;
        public const int Moves = 18;

        private readonly Game _game;

        private int _width;
        private int _height;
        
        private BitArray _blockPoints = new BitArray((1 << 17) - 1, false);
        private BitArray _groundPoints = new BitArray((1 << 17) - 1, false);
        private BitArray _jumpPoints = new BitArray((1 << 17) - 1, false);
        private BitArray _dropPoints = new BitArray((1 << 17) - 1, false);

        private BitArray _bottomPoints = new BitArray((1 << 17) - 1, false);
        private BitArray _ladderPoints = new BitArray((1 << 17) - 1, false);

        private BitArray _sharpLeftPoints = new BitArray((1 << 17) - 1, false);
        private BitArray _sharpRightPoints = new BitArray((1 << 17) - 1, false);

        private BitArray _goodMoves = new BitArray((1 << 28) - 1, false);

        public PointMaster(ref Game game)
        {
            _game = game;

            BuildPoints();
            CheckMoves();
        }

        public void BuildPoints()
        {
            var oldWidth = _game.Level.Tiles.Length;
            var oldHeight = _game.Level.Tiles[0].Length;
            _width = oldWidth * Divisor;
            _height = oldHeight * Divisor;

            for (var i = 0; i < oldWidth; ++i)
            for (var j = 0; j < oldHeight; ++j)
            {
                if (_game.Level.Tiles[i][j] == Tile.JumpPad)
                {
                    for (int i1 = Math.Max(0, i * Divisor - UnitHalfWidth); i1 <= Math.Min((i + 1) * Divisor + UnitHalfWidth, _width); ++i1)
                    for (int j1 = Math.Max(0, j * Divisor - UnitHeight); j1 <= (j + 1) * Divisor; ++j1)
                        _jumpPoints.Set(NavigationUtils.EncodePoint(i1, j1), true);
                }

                if (_game.Level.Tiles[i][j] == Tile.Ladder)
                {
                    for (int i1 = Math.Max(0, i * Divisor + 1), k = 0; i1 <= Math.Min((i + 1) * Divisor - 1, _width); ++i1, ++k)
                    for (int j1 = Math.Max(0, j * Divisor - UnitHalfHeight); j1 <= (j + 1) * Divisor; ++j1)
                    {
                        _groundPoints.Set(NavigationUtils.EncodePoint(i1, j1), true);
                        _dropPoints.Set(NavigationUtils.EncodePoint(i1, j1), true);
                        _ladderPoints.Set(NavigationUtils.EncodePoint(i1, j1), true);
                    }
                }

                if (_game.Level.Tiles[i][j] == Tile.Platform)
                {
                    for (int k = Math.Max(0, i * Divisor - UnitHalfWidth); k <= Math.Min((i + 1) * Divisor + UnitHalfWidth, _width); ++k)
                    {
                        _groundPoints.Set(NavigationUtils.EncodePoint(k, (j + 1) * Divisor), true);
                        _dropPoints.Set(NavigationUtils.EncodePoint(k, (j + 1) * Divisor), true);
                    }
                }

                if (_game.Level.Tiles[i][j] == Tile.Wall)
                {
                    for (int i1 = Math.Max(0, i * Divisor - UnitHalfWidth); i1 <= Math.Min((i + 1) * Divisor + UnitHalfWidth, _width); ++i1)
                    for (int j1 = Math.Max(0, j * Divisor - UnitHeight); j1 < (j + 1) * Divisor; ++j1)
                        _blockPoints.Set(NavigationUtils.EncodePoint(i1, j1), true);

                    var bottomExists = j * Divisor > UnitHeight;

                    for (int i1 = Math.Max(0, i * Divisor - UnitHalfWidth); i1 <= Math.Min((i + 1) * Divisor + UnitHalfWidth, _width); ++i1)
                    {
                        _groundPoints.Set(NavigationUtils.EncodePoint(i1, (j + 1) * Divisor), true);
                        if (bottomExists)
                            _bottomPoints.Set(NavigationUtils.EncodePoint(i1, j * Divisor - UnitHeight - 1), true);
                    }

                    if (bottomExists)
                    {
                        if (i * Divisor > UnitHalfWidth)
                            _sharpLeftPoints.Set(
                                NavigationUtils.EncodePoint(i * Divisor - UnitHalfWidth - 1,
                                    j * Divisor - UnitHeight - 1), true);

                        if ((i + 1) * Divisor < _width - UnitHalfWidth)
                            _sharpRightPoints.Set(
                                NavigationUtils.EncodePoint((i + 1) * Divisor + UnitHalfWidth + 1,
                                    j * Divisor - UnitHeight - 1), true);
                    }
                }
            }
        }

        public void CheckMoves()
        {
            for (int x = 0; x <= _width; ++x)
            for (int y = 0; y <= _height; ++y)
            {
                var point = NavigationUtils.EncodePoint(x, y);
                var offset = point * Moves;

                if (!IsPointJump(point))
                {
                    if (x > 0 && IsPointGround(point) && !IsPointBlocking(x - 1, y))
                        _goodMoves.Set(offset + 0, true);
                    if (x < _width && IsPointGround(point) && !IsPointBlocking(x + 1, y))
                        _goodMoves.Set(offset + 1, true);
                    if (x > 0 && y < _height && IsPointGround(point) && !IsSharpLeft(x - 1, y) && !IsPointBlocking(x - 1, y + 1))
                        _goodMoves.Set(offset + 2, true);
                    if (x < _width && y < _height && IsPointGround(point) && !IsSharpRight(x + 1, y) && !IsPointBlocking(x + 1, y + 1))
                        _goodMoves.Set(offset + 3, true);
                    if (y < _height && IsPointGround(point) && !IsPointBlocking(x, y + 1))
                        _goodMoves.Set(offset + 4, true);
                    if (x > 0 && y > 0 && (IsPointDrop(point) || !IsPointGround(point)) && !IsSharpRight(x, y - 1) && !IsPointBlocking(x - 1, y - 1))
                        _goodMoves.Set(offset + 5, true);
                    if (x < _width && y > 0 && (IsPointDrop(point) || !IsPointGround(point)) && !IsSharpLeft(x, y - 1) && !IsPointBlocking(x + 1, y - 1))
                        _goodMoves.Set(offset + 6, true);
                    if (y > 0 && (IsPointDrop(point) || !IsPointGround(point)) && !IsPointBlocking(x, y - 1))
                        _goodMoves.Set(offset + 7, true);
                    if (x > 0 && y < _height && !IsSharpLeft(x - 1, y) && !IsPointBlocking(x - 1, y + 1))
                        _goodMoves.Set(offset + 8, true);
                    if (x < _width && y < _height && !IsSharpRight(x + 1, y) && !IsPointBlocking(x + 1, y + 1))
                        _goodMoves.Set(offset + 9, true);
                    if (y < _height && !IsPointBlocking(x, y + 1))
                        _goodMoves.Set(offset + 10, true);
                    if (x > 0 && y + 1 < _height && !IsSharpLeft(x - 1, y) && !IsSharpLeft(x - 1, y + 1) && !IsPointBlocking(x - 1, y + 2))
                        _goodMoves.Set(offset + 14, true);
                    if (x < _width && y + 1 < _height && !IsSharpRight(x + 1, y) && !IsSharpRight(x + 1, y + 1) && !IsPointBlocking(x + 1, y + 2))
                        _goodMoves.Set(offset + 15, true);
                    if (y + 1 < _height && !IsPointBlocking(x, y + 2))
                        _goodMoves.Set(offset + 16, true);
                    if (IsPointBottom(x, y + 1))
                        _goodMoves.Set(offset + 17, true);
                }
                else
                {
                    if (x > 0 && y + 1 < _height && !IsSharpLeft(x - 1, y) && !IsSharpLeft(x - 1, y + 1) && !IsPointBlocking(x - 1, y + 2))
                        _goodMoves.Set(offset + 11, true);
                    if (x < _width && y + 1 < _height && !IsSharpRight(x + 1, y) && !IsSharpRight(x + 1, y + 1) && !IsPointBlocking(x + 1, y + 2))
                        _goodMoves.Set(offset + 12, true);
                    if (y + 1 < _height && !IsPointBlocking(x, y + 2))
                        _goodMoves.Set(offset + 13, true);
                }
            }
        }

        public void DebugDraw(Debug debug)
        {
            for (var i = 0; i < _width; ++i)
                debug.Draw(new CustomData.Line(
                    new Vec2Float(i, 0),
                    new Vec2Float(i, _height),
                    0.046f, new ColorFloat(0, 0.5f, 0, 0.5f)));
            for (var j = 0; j < _height; ++j)
                debug.Draw(new CustomData.Line(
                    new Vec2Float(0, j),
                    new Vec2Float(_width, j),
                    0.046f, new ColorFloat(0, 0.5f, 0, 0.5f)));

            /*for (int i = 0; i < _groundPoints.Count; ++i)
            {
                if (!_groundPoints[i])
                    continue;
                var (x, y) = NavigationUtils.DecodePoint(i);
                if (x <= Divisor + UnitHalfWidth || x >= _width - Divisor - UnitHalfWidth || y <= Divisor || y >= _height - Divisor - UnitHeight)
                    continue;
                debug.Draw(new CustomData.Rect(
                    new Vec2Float(1f * x / Divisor - 1f / Divisor, 1f * y / Divisor - 1f / Divisor),
                    new Vec2Float(2f / Divisor, 2f / Divisor),
                    new ColorFloat(1, 0, 1, 0.5f)));
            }

            for (int i = 0; i < _jumpPoints.Count; ++i)
            {
                if (!_jumpPoints[i])
                    continue;
                var (x, y) = NavigationUtils.DecodePoint(i);
                if (x <= Divisor + UnitHalfWidth || x >= _width - Divisor - UnitHalfWidth || y <= Divisor || y >= _height - Divisor - UnitHeight)
                    continue;
                debug.Draw(new CustomData.Rect(
                    new Vec2Float(1f * x / Divisor - 1f / Divisor, 1f * y / Divisor - 1f / Divisor),
                    new Vec2Float(2f / Divisor, 2f / Divisor),
                    new ColorFloat(1, 0, 0, 0.5f)));
            }

            for (int i = 0; i < _blockPoints.Count; ++i)
            {
                if (!_blockPoints[i])
                    continue;
                var (x, y) = NavigationUtils.DecodePoint(i);
                if (x <= Divisor + UnitHalfWidth || x >= _width - Divisor - UnitHalfWidth || y <= Divisor || y >= _height - Divisor - UnitHeight)
                    continue;
                debug.Draw(new CustomData.Rect(
                    new Vec2Float(1f * x / Divisor - 1f / Divisor, 1f * y / Divisor - 1f / Divisor),
                    new Vec2Float(2f / Divisor, 2f / Divisor),
                    new ColorFloat(0, 1, 1, 0.5f)));
            }*/
            /*for (int i = 0; i < _sharpLeftPoints.Count; ++i)
            {
                if (!_sharpLeftPoints[i])
                    continue;
                var (x, y) = NavigationUtils.DecodePoint(i);
                if (x <= Divisor + UnitHalfWidth || x >= _width - Divisor - UnitHalfWidth || y <= Divisor || y >= _height - Divisor - UnitHeight)
                    continue;
                debug.Draw(new CustomData.Rect(
                    new Vec2Float(1f * x / Divisor - 0.5f / Divisor, 1f * y / Divisor - 0.5f / Divisor),
                    new Vec2Float(1f / Divisor, 1f / Divisor),
                    new ColorFloat(0, 1, 1, 0.5f)));
            }
            for (int i = 0; i < _sharpRightPoints.Count; ++i)
            {
                if (!_sharpRightPoints[i])
                    continue;
                var (x, y) = NavigationUtils.DecodePoint(i);
                if (x <= Divisor + UnitHalfWidth || x >= _width - Divisor - UnitHalfWidth || y <= Divisor || y >= _height - Divisor - UnitHeight)
                    continue;
                debug.Draw(new CustomData.Rect(
                    new Vec2Float(1f * x / Divisor - 1f / Divisor, 1f * y / Divisor - 1f / Divisor),
                    new Vec2Float(2f / Divisor, 2f / Divisor),
                    new ColorFloat(0, 1, 1, 0.5f)));
            }*/
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsPointBlocking(int x, int y) => _blockPoints.Get(NavigationUtils.EncodePoint(x, y));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsPointBlocking(int point) => _blockPoints.Get(point);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsPointGround(int x, int y) => _groundPoints.Get(NavigationUtils.EncodePoint(x, y));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsPointGround(int point) => _groundPoints.Get(point);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsPointJump(int x, int y) => _jumpPoints.Get(NavigationUtils.EncodePoint(x, y));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsPointJump(int point) => _jumpPoints.Get(point);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsPointDrop(int x, int y) => _dropPoints.Get(NavigationUtils.EncodePoint(x, y));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsPointDrop(int point) => _dropPoints.Get(point);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsPointBottom(int x, int y) => _bottomPoints.Get(NavigationUtils.EncodePoint(x, y));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsPointBottom(int point) => _bottomPoints.Get(point);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSharpLeft(int x, int y) => _sharpLeftPoints.Get(NavigationUtils.EncodePoint(x, y));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSharpLeft(int point) => _sharpLeftPoints.Get(point);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSharpRight(int x, int y) => _sharpRightPoints.Get(NavigationUtils.EncodePoint(x, y));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSharpRight(int point) => _sharpRightPoints.Get(point);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsPointLadder(int x, int y) => _ladderPoints.Get(NavigationUtils.EncodePoint(x, y));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsPointLadder(int point) => _ladderPoints.Get(point);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsValid(int x, int y) => x >= 0 && x <= _width && y >= 0 && y <= _height;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsValidMove(int x, int y, int move) =>
            _goodMoves.Get(NavigationUtils.EncodePoint(x, y) * Moves + move);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsValidMove(int point, int move) => _goodMoves.Get(point * Moves + move);
    }
}