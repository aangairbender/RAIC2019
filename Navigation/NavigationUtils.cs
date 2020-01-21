using System.Runtime.CompilerServices;

namespace AiCup2019.Navigation
{
    public static class NavigationUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int EncodePoint(int x, int y) => (x << 9)  + y;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (int, int) DecodePoint(int hash) => (hash >> 9, hash & ((1 << 9) - 1));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int EncodeState(int x, int y, int jumpTicks, int fromJumpPad)
        {
            return
                x // 9 bits
                + (y << 9) // 9 bits
                + (jumpTicks << 18) // 6 bits
                + (fromJumpPad << 24);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (int, int, int, int) DecodeState(int code)
        {
            return (
                code & ((1 << 9) - 1),
                (code >> 9) & ((1 << 9) - 1),
                (code >> 18) & ((1 << 6) - 1),
                (code >> 24) & 1);
        }
    }
}