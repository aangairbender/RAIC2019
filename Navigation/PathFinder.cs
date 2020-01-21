using System;
using System.Collections.Generic;
using System.Linq;

namespace AiCup2019.Navigation
{
    public class PathFinder
    {
        private readonly PointMaster _pointMaster;

        private List<int> _lastPath;
        private int _lastFx, _lastFy;

        public PathFinder(PointMaster pointMaster)
        {
            _pointMaster = pointMaster;
        }

        public List<int> FindPath(int s, int fx, int fy, int dist = 0)
        {
            if (_lastPath != null && fx == _lastFx && fy == _lastFy && _lastPath.Count > 0)
            {
                var (sx, sy, jj, pp) = NavigationUtils.DecodeState(s);
                var (sx1, sy1, jj1, pp1) = NavigationUtils.DecodeState(_lastPath[0]);
                if (sx == sx1 && sy == sy1)
                {
                    _lastPath.RemoveAt(0);
                    return _lastPath;
                }
            }

            var order = new LinkedList<int>();
            order.AddLast(s);

            var parent = new Dictionary<int, int>();
            parent.Add(s, s);
            int f = -1;
            while (order.Count > 0)
            {
                var currentCode = order.First.Value;
                order.RemoveFirst();
                var (x, y, j, p) = NavigationUtils.DecodeState(currentCode);
                if (Math.Abs(x - fx) <= dist && Math.Abs(y  - fy) <= dist)
                {
                    f = currentCode;
                    break;
                }

                var currentPoint = NavigationUtils.EncodePoint(x, y);
                for (int move = 0; move < PointMaster.Moves; ++move)
                {
                    if (!_pointMaster.IsValidMove(currentPoint, move))
                        continue;
                    var newCode = ApplyMove(x, y, j, p, move);
                    if (newCode < 0 || parent.ContainsKey(newCode))
                        continue;
                    var (nx, ny, nj, np) = NavigationUtils.DecodeState(newCode);
                    parent[newCode] = currentCode;
                    order.AddLast(newCode);
                }

                if (j == 1 && p == 1)
                {
                    var newCode = NavigationUtils.EncodeState(x, y, 0, 0);
                    if (newCode < 0 || parent.ContainsKey(newCode))
                        continue;
                    parent[newCode] = currentCode;
                    order.AddLast(newCode);
                }
            }

            if (f == -1)
                return null;

            var path = new List<int>();
            var cur = f;
            while (cur != s)
            {
                cur = parent[cur];
                if (cur == s)
                    break;
                path.Add(cur);
            }
            path.Reverse();


            order.Clear();
            parent.Clear();

            _lastFx = fx;
            _lastFy = fy;
            _lastPath = path;

            return path;
        }

        private int ApplyMove(int x, int y, int j, int p, int move)
        {
            if (move <= 5 && j != 0)
                return -1;
            if (move >= 6 && move <= 7 && p != 0)
                return -1;
            if (move >= 8 && move <= 10 && !(j > 0 && p == 0))
                return -1;
            if (move >= 14 && !(j > 1 && p == 1))
                return -1;

            switch (move)
            {
                case 0: return NavigationUtils.EncodeState(x - 1, y, 0, 0);
                case 1: return NavigationUtils.EncodeState(x + 1, y, 0, 0);

                case 2: return NavigationUtils.EncodeState(x - 1, y + 1, 32, 0);
                case 3: return NavigationUtils.EncodeState(x + 1, y + 1, 32, 0);
                case 4: return NavigationUtils.EncodeState(x, y + 1, 32, 0);

                case 5: return NavigationUtils.EncodeState(x - 1, y - 1, 0, 0);
                case 6: return NavigationUtils.EncodeState(x + 1, y - 1, 0, 0);
                case 7: return NavigationUtils.EncodeState(x, y - 1, 0, 0);

                case 8: return NavigationUtils.EncodeState(x - 1, y + 1, j - 1, 0);
                case 9: return NavigationUtils.EncodeState(x + 1, y + 1, j - 1, 0);
                case 10: return NavigationUtils.EncodeState(x, y + 1, j - 1, 0);

                case 11: return NavigationUtils.EncodeState(x - 1, y + 2, 31, 1);
                case 12: return NavigationUtils.EncodeState(x + 1, y + 2, 31, 1);
                case 13: return NavigationUtils.EncodeState(x, y + 2, 31, 1);

                case 14: return NavigationUtils.EncodeState(x - 1, y + 2, j - 1, 1);
                case 15: return NavigationUtils.EncodeState(x + 1, y + 2, j - 1, 1);
                case 16: return NavigationUtils.EncodeState(x, y + 2, j - 1, 1);

                case 17: return NavigationUtils.EncodeState(x, y, 0, 0);
                default:
                    //impossible
                    return -1;
            }
        }

        private IEnumerable<int> FindWalkMoves(int x, int y, int j, int p)
        {
            if (!_pointMaster.IsPointGround(x, y) || j > 0)
                yield break;

            /*if (_pointMaster.IsPointGround(x - 1, y))
            {
                yield return NavigationUtils.EncodeState(x - 1, y, 0, 0);
                if (_pointMaster.IsPointGround(x - 2, y))
                    yield return NavigationUtils.EncodeState(x - 2, y, 0, 0);
            }

            if (_pointMaster.IsPointGround(x + 1, y))
            {
                yield return NavigationUtils.EncodeState(x + 1, y, 0, 0);
                if (_pointMaster.IsPointGround(x + 2, y))
                    yield return NavigationUtils.EncodeState(x + 2, y, 0, 0);
            }*/
            yield return NavigationUtils.EncodeState(x - 2, y, 0, 0);
            yield return NavigationUtils.EncodeState(x - 1, y, 0, 0);
            yield return NavigationUtils.EncodeState(x + 1, y, 0, 0);
            yield return NavigationUtils.EncodeState(x + 2, y, 0, 0);
        }

        private IEnumerable<int> FindUsualFlyUpMoves(int x, int y, int j, int p)
        {
            if (p != 0 || (j == 0 && !_pointMaster.IsPointGround(x, y)))
                yield break;

            if (j == 0)
                j = 33;
                
            /*if (!_pointMaster.IsPointBlocking(x, y + 1))
            {
                yield return NavigationUtils.EncodeState(x, y + 2, j - 1, 0);
                 yield return NavigationUtils.EncodeState(x - 1, y + 2, j - 1, 0);
                yield return NavigationUtils.EncodeState(x + 1, y + 2, j - 1, 0);
            }

            if (!_pointMaster.IsPointBlocking(x - 1, y + 1))
                yield return NavigationUtils.EncodeState(x - 2, y + 2, j - 1, 0);

            if (!_pointMaster.IsPointBlocking(x + 1, y + 1))
                yield return NavigationUtils.EncodeState(x + 2, y + 2, j - 1, 0);*/

            /*if (!_pointMaster.IsSharp(x, y + 1))
            {
                if (!_pointMaster.IsSharp(x - 1, y + 2) && !_pointMaster.IsPointBlocking(x - 1, y + 1))
                    yield return NavigationUtils.EncodeState(x - 2, y + 2, j - 1, 0);

                if (!_pointMaster.IsSharp(x + 1, y + 2) && !_pointMaster.IsPointBlocking(x + 1, y + 1))
                    yield return NavigationUtils.EncodeState(x + 2, y + 2, j - 1, 0);
            }*/

            yield return NavigationUtils.EncodeState(x - 2, y + 2, j - 1, 0);
            yield return NavigationUtils.EncodeState(x - 1, y + 2, j - 1, 0);
            yield return NavigationUtils.EncodeState(x    , y + 2, j - 1, 0);
            yield return NavigationUtils.EncodeState(x + 1, y + 2, j - 1, 0);
            yield return NavigationUtils.EncodeState(x + 2, y + 2, j - 1, 0);
        }

        private IEnumerable<int> FindUsualFlyDownMoves(int x, int y, int j, int p)
        {
            if (!(j > 0 && p == 0) || (_pointMaster.IsPointGround(x, y) && !_pointMaster.IsPointDrop(x, y)))
                yield break;

            /*if (!_pointMaster.IsPointBlocking(x, y - 1))
            {
                yield return NavigationUtils.EncodeState(x, y - 2, 0, 0);
                if (_pointMaster.IsPointGround(x, y - 1))
                    yield return NavigationUtils.EncodeState(x, y - 1, 0, 0);
            }

            if (!_pointMaster.IsSharp(x - 1, y - 1))
            {
                if (!_pointMaster.IsPointBlocking(x - 1, y - 1))
                {
                    yield return NavigationUtils.EncodeState(x - 1, y - 2, 0, 0);
                    if (!_pointMaster.IsSharp(x - 1, y))
                    {
                        if (!_pointMaster.IsSharp(x - 2, y - 1))
                            yield return NavigationUtils.EncodeState(x - 2, y - 2, 0, 0);
                        else if (_pointMaster.IsPointGround(x - 1, y - 1))
                            yield return NavigationUtils.EncodeState(x - 1, y - 1, 0, 0);
                    }
                }

            }

            if (!_pointMaster.IsSharp(x + 1, y - 1))
            {
                if (!_pointMaster.IsPointBlocking(x + 1, y - 1))
                {
                    yield return NavigationUtils.EncodeState(x + 1, y - 2, 0, 0);
                    if (!_pointMaster.IsSharp(x + 1, y))
                    {
                        if (!_pointMaster.IsSharp(x + 2, y - 1))
                            yield return NavigationUtils.EncodeState(x - 2, y - 2, 0, 0);
                        else if (_pointMaster.IsPointGround(x + 1, y - 1))
                            yield return NavigationUtils.EncodeState(x + 1, y - 1, 0, 0);
                    }
                }
            }*/

            yield return NavigationUtils.EncodeState(x - 2, y - 2, 0, 0);
            yield return NavigationUtils.EncodeState(x - 1, y - 2, 0, 0);
            yield return NavigationUtils.EncodeState(x    , y - 2, 0, 0);
            yield return NavigationUtils.EncodeState(x + 1, y - 2, 0, 0);
            yield return NavigationUtils.EncodeState(x + 2, y - 2, 0, 0);
        }

        private IEnumerable<int> FindMoves(int state)
        {
            var (x, y, j, p) = NavigationUtils.DecodeState(state);

            foreach (var findWalkMove in FindWalkMoves(x, y, j, p))
            {
                yield return findWalkMove;
            }

            foreach (var findUsualFlyUpMove in FindUsualFlyUpMoves(x, y, j, p))
            {
                yield return findUsualFlyUpMove;
            }

            foreach (var findUsualFlyDownMove in FindUsualFlyDownMoves(x, y, j, p))
            {
                yield return findUsualFlyDownMove;
            }

        }

        /*private IEnumerable<int> FindMoves2(int state)
        {
            var (x, y, j, p) = NavigationUtils.DecodeState(state);

            if (_pointMaster.IsPointJump(x, y))
            {
                yield return NavigationUtils.EncodeState(x, y + 4, 31, 1);
                if (CanJump(x, y, -1, 1))
                    yield return NavigationUtils.EncodeState(x - 1, y + 4, 31, 1);
                if (CanJump(x, y, +1, 1))
                    yield return NavigationUtils.EncodeState(x + 1, y + 4, 31, 1);
                if (CanJump(x, y, -2, 1))
                    yield return NavigationUtils.EncodeState(x - 2, y + 4, 31, 1);
                if (CanJump(x, y, +2, 1))
                    yield return NavigationUtils.EncodeState(x + 2, y + 4, 31, 1);
                yield break;
            }

            if (j > 0)
            {
                if (j == 1 && p == 1)
                {
                    if (!_pointMaster.IsPointBlocking(x, y + 2))
                        yield return NavigationUtils.EncodeState(x, y + 1, 0, 0);
                    if (!_pointMaster.IsPointBlocking(x - 1, y + 2) && !_pointMaster.IsPointBlocking(x, y + 2))
                        yield return NavigationUtils.EncodeState(x - 1, y + 1, 0, 0);
                    if (!_pointMaster.IsPointBlocking(x + 1, y + 2) && !_pointMaster.IsPointBlocking(x, y + 2))
                        yield return NavigationUtils.EncodeState(x + 1, y + 1, 0, 0);
                    if (!_pointMaster.IsPointBlocking(x - 1, y + 2))
                        yield return NavigationUtils.EncodeState(x - 2, y + 1, 0, 0);
                    if (!_pointMaster.IsPointBlocking(x + 1, y + 2))
                        yield return NavigationUtils.EncodeState(x + 2, y + 1, 0, 0);
                    yield break;
                }
                var jSpd = (p == 1 ? 4 : 2);
                yield return NavigationUtils.EncodeState(x, y + jSpd, j - 1, p);
                if (CanJump(x, y, -1, p))
                    yield return NavigationUtils.EncodeState(x - 1, y + jSpd, j - 1, p);
                if (CanJump(x, y, 1, p))
                    yield return NavigationUtils.EncodeState(x + 1, y + jSpd, j - 1, p);
                if (CanJump(x, y, -2, p))
                    yield return NavigationUtils.EncodeState(x - 2, y + jSpd, j - 1, p);
                if (CanJump(x, y, 2, p))
                    yield return NavigationUtils.EncodeState(x + 2, y + jSpd, j - 1, p);

                if (p == 0)
                {
                    yield return NavigationUtils.EncodeState(x, y - 2, 0, 0);
                    yield return NavigationUtils.EncodeState(x - 1, y - 2, 0, 0);
                    yield return NavigationUtils.EncodeState(x + 1, y - 2, 0, 0);
                    yield return NavigationUtils.EncodeState(x - 2, y - 2, 0, 0);
                    yield return NavigationUtils.EncodeState(x + 2, y - 2, 0, 0);
                }

                yield break;
            }

            if (_pointMaster.IsPointGround(x, y))
            {
                yield return NavigationUtils.EncodeState(x, y, 0, 0);
                yield return NavigationUtils.EncodeState(x - 1, y, 0, 0);
                yield return NavigationUtils.EncodeState(x + 1, y, 0, 0);
                yield return NavigationUtils.EncodeState(x - 2, y, 0, 0);
                yield return NavigationUtils.EncodeState(x + 2, y, 0, 0);

                yield return NavigationUtils.EncodeState(x, y + 2, 32, 0);
                if (CanJump(x, y, -1, 0))
                    yield return NavigationUtils.EncodeState(x - 1, y + 2, 32, 0);
                if (CanJump(x, y, 1, 0))
                    yield return NavigationUtils.EncodeState(x + 1, y + 2, 32, 0);
                if (CanJump(x, y, -2, 0))
                    yield return NavigationUtils.EncodeState(x - 2, y + 2, 32, 0);
                if (CanJump(x, y, -2, 0))
                    yield return NavigationUtils.EncodeState(x + 2, y + 2, 32, 0);

                if (!_pointMaster.IsPointDrop(x, y))
                    yield break;
            }

            // free fall
            if (_pointMaster.IsPointGround(x, y - 1))
                yield return NavigationUtils.EncodeState(x, y - 1, 0, 0);
            else
                yield return NavigationUtils.EncodeState(x, y - 2, 0, 0);

            if (!_pointMaster.IsPointGround(x, y - 1))
            {
                if (!_pointMaster.IsPointGround(x - 1, y - 1))
                    yield return NavigationUtils.EncodeState(x - 1, y - 2, 0, 0);
                if (!_pointMaster.IsPointGround(x + 1, y - 1))
                    yield return NavigationUtils.EncodeState(x + 1, y - 2, 0, 0);
            }

            if (_pointMaster.IsPointGround(x - 1, y - 1))
                yield return NavigationUtils.EncodeState(x - 1, y - 1, 0, 0);
            else
                yield return NavigationUtils.EncodeState(x - 2, y - 2, 0, 0);


            if (_pointMaster.IsPointGround(x + 1, y - 1))
                yield return NavigationUtils.EncodeState(x + 1, y - 1, 0, 0);
            else
                yield return NavigationUtils.EncodeState(x + 2, y - 2, 0, 0);

            //TODO: add ladder
        }*/

        /*private bool CanJump(int x, int y, int dx, int p)
        {
            if (_pointMaster.IsSharp(x, y + 1))
                return false;
            if (Math.Abs(dx) == 2 && _pointMaster.IsPointBlocking(x + dx / 2, y + (p==1 ? 2 : 1)))
                return false;
            if (p == 0)
            {
                if (dx == -2 && _pointMaster.IsSharp(x - 1, y + 2))
                    return false;
                if (dx == 2 && _pointMaster.IsSharp(x + 1, y + 2))
                    return false;
                return true;
            }

            if (p == 1)
            {
                if (dx == -2 && _pointMaster.IsSharp(x - 1, y + 3))
                    return false;
                if (dx == 2 && _pointMaster.IsSharp(x + 1, y + 3))
                    return false;
                if (_pointMaster.IsSharp(x, y + 2))
                    return false;
            }
            return true;
        }*/
    }
}