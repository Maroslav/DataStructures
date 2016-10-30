using System;

namespace Utils.DataStructures.SplayTree
{
    internal class FlipBase<TDoFlipTrait>
    {
        public static bool FlipChildren;

        static FlipBase()
        {
            if (typeof(TDoFlipTrait) == typeof(NoFlip))
                FlipChildren = false;
            else if (typeof(TDoFlipTrait) == typeof(DoFlip))
                FlipChildren = true;
            else
                throw new TypeLoadException(string.Format("Invalid type parameter {0} for the FlipBase class.", typeof(TDoFlipTrait).Name));
        }
    }

    internal sealed class NoFlip
        : FlipBase<NoFlip>
    { }

    internal sealed class DoFlip
        : FlipBase<DoFlip>
    { }
}
