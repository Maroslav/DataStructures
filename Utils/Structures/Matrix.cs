using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Utils.Structures
{
    public class Matrix<T>
        where T : struct
    {
        private T[] _elements;


        public int Width { get; private set; }
        public int Height { get; private set; }

        internal T[] Elements
        {
            get { return _elements; }
        }


        #region Genesis

#if NONCLEAN
        // 
        internal Action<string> SwapCallback = s => { }; // Do nothing by default

        public Matrix(Action<string> swapCallback, int width, int height)
            : this(width, height)
        {
            SwapCallback = swapCallback;
            SwapCallback(string.Format("N {0}", width)); // Width should be height
        }
#endif

        public Matrix(Matrix<T> other)
            : this(other._elements, other.Width, other.Height)
        { }

        internal Matrix(T[] elements, int width, int height)
            : this(width, height)
        {
            if (elements == null)
                throw new ArgumentNullException("elements");

            if (width * height != elements.Length)
                throw new ArgumentOutOfRangeException("width", "The provided array has invalid size.");

            Debug.Assert(_elements.Length == elements.Length);
            Buffer.BlockCopy(elements, 0, _elements, 0, elements.Length * Marshal.SizeOf(typeof(T)));
        }

        public Matrix(int size)
            : this(size, size)
        { }

        public Matrix(int width, int height)
        {
            if (width <= 0 || height <= 0)
                throw new ArgumentOutOfRangeException(width <= 0 ? "width" : "height", "All matrix dimensions must be positive.");

            Width = width;
            Height = height;

            _elements = new T[width * height];
        }

        #endregion

        #region Indexing

        public T this[int column, int row]
        {
            get { return _elements[row * Width + column]; }
            set { _elements[row * Width + column] = value; }
        }

        #endregion

        #region Public methods

        public void Transpose()
        {
            if (Width != Height)
                throw new NotImplementedException("Cannot transpose non-square matrices.");

            int size = Width * Height;

            if (size == 1)
                return;

            if (size == 4)
            {
                Swap(1, 2);
                return;
            }

#if __NAIVE
            TransposeInternalNaive();
#else
            TransposeInternal();
#endif

#if NONCLEAN
            SwapCallback("E");
#endif
        }

        #endregion

        #region Helpers

        #region Submatrix dims helper struct

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

#if VERBOSE
                Debug.WriteLine("Split: {0},\t\thalf: {1}::{2}", this, halfWidth, halfHeight);
#endif
            }

            public override string ToString()
            {
                return string.Format("lr: {0}::{1},\t\ttb: {2}::{3}", X, X + Width, Y, Y + Height);
            }
        }

        #endregion

        #region Complex

        private const int NaiveThreshold = 8 * 8;

        internal void TransposeInternal()
        {
            if (Width * Height < NaiveThreshold)
            {
                TransposeInternalNaive();
            }
            else
            {
                var dims = new SubmatrixDims(0, 0, Width, Height);
                TransposeInternal(ref dims);
            }
        }

        // The recursion ends when the split submatrices are small enough. This saves us
        // a lot of work caused by recursion. We do the recursion end check before recursing 
        // instead of at the start of the recursive func to reduce the recursion depth by one
        // (it can further save us a lot of recursive calls -- with the tree branching 
        // factor of ~4 the number of leaves in the recursion tree is very high compared to
        // the number of internal nodes.
        private void TransposeInternal(ref SubmatrixDims dims)
        {
            // 1. Prepare the submatrices
            SubmatrixDims a11, a21, a12, a22;
            dims.SplitDims(out a11, out a21, out a12, out a22);

            // 2. If the matrices are small enough, transpose them naively
            if (a11.Width * a11.Height <= NaiveThreshold)
            {
                TransposeInternalNaive(ref a11);
                TransposeInternalNaive(ref a22);
                TransposeAndSwapNaive(ref a21, ref a12);
                return;
            }

            // 3. Recurse on the submatrices
            TransposeInternal(ref a11);
            TransposeInternal(ref a22);
            TransposeAndSwap(ref a21, ref a12);
        }

        private void TransposeAndSwap(ref SubmatrixDims a, ref SubmatrixDims b)
        {
            Debug.Assert(a.Width == b.Height && a.Height == b.Width);

            // 1. Prepare the submatrices
            SubmatrixDims a11, a21, a12, a22;
            SubmatrixDims b11, b21, b12, b22;
            a.SplitDims(out a11, out a21, out a12, out a22);
            b.SplitDims(out b11, out b21, out b12, out b22);

            // 2. If the matrices are small, transpose them naively
            // Check just the first one; because we have a square matrix, the other sizes should be very similar
            if (a11.Width * a11.Height < NaiveThreshold)
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

        internal void TransposeInternalNaive()
        {
            var dims = new SubmatrixDims(0, 0, Width, Height);
            TransposeInternalNaive(ref dims);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void TransposeInternalNaive(ref SubmatrixDims dims)
        {
            Debug.Assert(dims.Width == dims.Height);

            int baseOffset = dims.Y * Width + dims.X; // Pointer to the right half (triangle above the diagonal)
            int baseTransOffset = baseOffset; // Pointer to the left half (below the diagonal)
            int xSkip = 0; // The amount of columns (rows for the left half) to skip

            for (int y = baseTransOffset + Width; y < baseTransOffset + dims.Height * Width; y += Width) // Iterate over left half's rows 
            {
                // baseTransOffset starts with the second row, while baseOffset starts at the first row (second element)
                int transOffset = y + xSkip; // Reset to the next line's beginning; offset the column by on less than the right pointer
                xSkip++;

                for (int offset = baseOffset + xSkip; offset < baseOffset + dims.Width; offset++) // Iterate over right half's columns
                {
                    Swap(offset, transOffset);
                    transOffset += Width; // Move by a line
                }

                baseOffset += Width;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void TransposeAndSwapNaive(ref SubmatrixDims a, ref SubmatrixDims b)
        {
#if VERBOSE
            Debug.WriteLine("a: {0}", a);
            Debug.WriteLine(ToString(a));
            Debug.WriteLine("b: {0}", b);
            Debug.WriteLine(ToString(b));
#endif
            Debug.Assert(a.Width == b.Height && a.Height == b.Width);

            int baseOffset = a.Y * Width + a.X; // Pointer to a's top-left corner
            int baseTransOffset = b.Y * Width + b.X; // Pointer to b's top-left corner

            for (int y = baseTransOffset; y < baseTransOffset + b.Width; y++) // Iterate over b's columns
            {
                var transOffset = y; // Reset transOffset to the first line

                for (int offset = baseOffset; offset < baseOffset + a.Width; offset++) // Iterate over a's columns
                {
                    Swap(offset, transOffset);
                    transOffset += Width; // Move by a line
                }

                baseOffset += Width; // Jump to the next line
            }
        }

        internal void TransposeAndSwapTest()
        {
            var dims = new SubmatrixDims(0, 0, Width, Height);
            TransposeAndSwapNaive(ref dims, ref dims);
            // The result should be the original matrix
        }

        #endregion


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Swap(int idxOne, int idxTwo)
        {
#if VERBOSE
                    Debug.WriteLine("Swap: {0}::{1}", idxOne, idxTwo);
#endif
#if NONCLEAN
            int r1 = idxOne % Width;
            int s1 = idxOne / Width;
            int r2 = idxTwo % Width;
            int s2 = idxTwo / Width;
            SwapCallback(string.Format("X {0} {1} {2} {3}", r1, s1, r2, s2));
#endif
            Swap(ref _elements[idxOne], ref _elements[idxTwo]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void Swap<TT>(ref TT one, ref TT two)
        {
            TT tmp = one;
            one = two;
            two = tmp;
        }

        #endregion

        #region ToString

        public override string ToString()
        {
            var sb = new StringBuilder();

            for (int j = 0; j < Height; j++)
            {
                for (int i = 0; i < Width; i++)
                {
                    sb.Append(this[i, j]);
                    sb.Append('\t');
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }

        private string ToString(SubmatrixDims dims)
        {
            var sb = new StringBuilder();

            for (int j = dims.Y; j < dims.Y + dims.Height; j++)
            {
                for (int i = dims.X; i < dims.X + dims.Width; i++)
                {
                    sb.Append(this[i, j]);
                    sb.Append('\t');
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }

        #endregion
    }
}
