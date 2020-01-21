using AiCup2019.Model;

namespace AiCup2019.Helpers
{
    public static class VecExtensions
    {
        public static Vec2Double ToVec2Double(this Vec2Float vec2f)
        {
            return new Vec2Double(vec2f.X, vec2f.Y);
        }

        public static Vec2Float ToVec2Double(this Vec2Double vec2d)
        {
            return new Vec2Float((float)vec2d.X, (float)vec2d.Y);
        }
    }
}