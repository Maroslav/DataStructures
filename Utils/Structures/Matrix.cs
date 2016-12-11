using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Utils.Structures
{
    public class Matrix<T>
        where T : struct
    {
        private T[] _elements;


        public int Width { get; private set; }
        public int Height { get; private set; }

        public T[] Elements
        {
            get { return _elements; }
        }


        public Matrix(int width, int height)
        {
            if (width <= 0 || height <= 0)
                throw new ArgumentOutOfRangeException(width <= 0 ? "width" : "height", "All matrix dimensions must be positive.");

            Width = width;
            Height = height;

            _elements = new T[width * height];
        }


        #region Indexing

        public T this[int column, int row]
        {
            get { return _elements[row * Width + column]; }
            protected set { _elements[row * Width + column] = value; }
        }

        #endregion

        #region Public methods

        public void Transpose()
        {
            if (Width != Height)
                throw new NotImplementedException("Cannot transpose non-square matrices.");

#if __NAIVE
            TransposeInternalNaive();
#else
            TransposeInternal();
#endif
        }

        #endregion

        #region Helpers

        #region Submatrix size helper struct

        [StructLayout(LayoutKind.Sequential, Pack = 2)]
        private struct SubmatrixDims
        {
            public int X, Y;
            public int Width, Height;


            public SubmatrixDims(int x, int y, int width, int height)
            {
                X = x;
                Y = y;
                Width = width;
                Height = height;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SplitDims(
               out SubmatrixDims a11,
               out SubmatrixDims a21,
               out SubmatrixDims a12,
               out SubmatrixDims a22)
            {
                int halfWidth = Width >> 1;
                int halfHeight = Height >> 1;

                // Left column
                a11.X = a12.X = X;
                a11.Width = a12.Width = halfWidth;
                // Right column
                a21.X = a22.X = X + halfWidth;
                a21.Width = a22.Width = Width - halfWidth;

                // Top row
                a11.Y = a21.Y = Y;
                a11.Height = a21.Height = halfHeight;
                // Bottom row
                a12.Y = a22.Y = Y + halfHeight;
                a12.Height = a22.Height = Height - halfHeight;
            }
        }

        #endregion

        #region Complex

        private const int NaiveThreshold = 64;

        private void TransposeInternal()
        {
            var dims = new SubmatrixDims(0, 0, Width, Height);
            TransposeInternal(ref dims);
        }

        // We do the check before recursing instead of at the start of the recursive func 
        // to reduce the recursion depth by one (it can save us nearly 3/4 of recursive calls)
        private void TransposeInternal(ref SubmatrixDims dims)
        {
            // 1. Prepare the submatrices
            SubmatrixDims a11, a21, a12, a22;
            dims.SplitDims(out a11, out a21, out a12, out a22);

            // 2. If the matrices are small enough, transpose them naively
            if (a11.Width * a11.Height <= NaiveThreshold)
            {
                TransposeInternalNaive(ref a11);
                TransposeInternalNaive(ref a21);
                TransposeInternalNaive(ref a12);
                TransposeInternalNaive(ref a22);
                return;
            }

            // 3. Recurse on the submatrices
            TransposeInternal(ref a11);
            TransposeInternal(ref a22);
            TransposeAndSwap(ref a21, ref a12);
        }

        private void TransposeAndSwap(ref SubmatrixDims a, ref SubmatrixDims b)
        {
            // 1. Prepare the submatrices
            SubmatrixDims a11, a21, a12, a22;
            SubmatrixDims b11, b21, b12, b22;
            a.SplitDims(out a11, out a21, out a12, out a22);
            b.SplitDims(out b11, out b21, out b12, out b22);

            // 2. If the matrices are small, transpose them naively
            Debug.Assert(a.Width == b.Width && a.Height == b.Height);
            if (a.Width * a.Height < NaiveThreshold)
            {
                TransposeAndSwapNaive(ref a11, ref b11);
                TransposeAndSwapNaive(ref a21, ref b12);
                TransposeAndSwapNaive(ref a12, ref b21);
                TransposeAndSwapNaive(ref a22, ref b22);
                return;
            }

            // 3. Transpose and swap them
            TransposeAndSwap(ref a11, ref b11);
            TransposeAndSwap(ref a21, ref b12);
            TransposeAndSwap(ref a12, ref b21);
            TransposeAndSwap(ref a22, ref b22);
        }

        #endregion

        #region Naive

        private void TransposeInternalNaive()
        {
            var dims = new SubmatrixDims(0, 0, Width, Height);
            TransposeInternalNaive(ref dims);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void TransposeInternalNaive(ref SubmatrixDims dims)
        {
            Debug.Assert(dims.Width == dims.Height);

            int baseOffset = dims.Y * Width + dims.X; // Pointer to the right half (triangle above the diagonal)
            int baseTransOffset = dims.X * Width + dims.Y; // Pointer to the left half (below the diagonal)
            int xSkip = 0; // The amount of columns (rows for the left half) to skip

            for (int y = baseTransOffset; y < baseTransOffset + dims.Height * Width; y += Width) // Iterate over left half's rows
            {
                xSkip++;
                int transOffset = baseTransOffset; // Reset to the next line's beginning

                for (int offset = baseOffset + xSkip; offset < baseOffset + dims.Width; offset++) // Iterate over right half's columns
                {
                    Swap(ref _elements[offset], ref _elements[transOffset]);
                    transOffset += Width; // Move by a line
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void TransposeAndSwapNaive(ref SubmatrixDims a, ref SubmatrixDims b)
        {
            Debug.Assert(a.Width == b.Height && a.Height == b.Width);

            int baseOffset = a.Y * Width + a.X; // Pointer to a's top-left corner
            int baseTransOffset = b.Y * Width + b.X; // Pointer to b's top-left corner

            for (int y = baseTransOffset; y < baseTransOffset + b.Width; y++) // Iterate over b's columns
            {
                var transOffset = y; // Reset transOffset to the first line

                for (int offset = baseOffset; offset < baseOffset + a.Width; offset++) // Iterate over a's columns
                {
                    Swap(ref _elements[offset], ref _elements[transOffset]);
                    transOffset += Width; // Move by a line
                }

                baseOffset += Width; // Jump to the next line
            }
        }

        #endregion


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void Swap<TT>(ref TT one, ref TT two)
        {
            TT tmp = one;
            one = two;
            two = tmp;
        }

        #endregion
    }
}
