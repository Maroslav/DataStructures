using System;
using System.Runtime.CompilerServices;

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

#if DEBUG
            TransposeInternalNaive();
#else
            TransposeInternal();
#endif
        }

        #endregion

        #region Helpers

        private void TransposeInternal()
        {
        }

        private void TransposeInternalNaive()
        {
            for (int j = 0; j < Height; j++)
                for (int i = 0; i < Width; i++)
                    Swap(ref _elements[j * Width + i], ref _elements[i * Height + j]);
        }

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
