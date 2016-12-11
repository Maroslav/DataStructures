using System;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Utils.Structures;

namespace UtilsTests.Matrix
{
    [TestClass]
    public class MatrixTests
    {
        private const int MatrixSize = 400;

        Matrix<int> _m = new Matrix<int>(MatrixSize, MatrixSize);


        public MatrixTests()
        {
            //for (int j = 0; j < _m.Height; j++)
            //    for (int i = 0; i < _m.Width; i++)
            //        _m[i, j] = j * _m.Width + i;

            for (int i = 0; i < _m.Width * _m.Height; i++)
                _m.Elements[i] = i;
        }

        private void AssertMatrixEqual(Matrix<int> one, Matrix<int> other)
        {
            Assert.AreEqual(one.Width, other.Width);
            Assert.AreEqual(one.Height, other.Height);

            if (one.Width * one.Height < 400)
            {
                Debug.WriteLine(">>Orig:");
                Debug.WriteLine(one);
                Debug.WriteLine(">>Trans:");
                Debug.WriteLine(other);
            }

            for (int j = 0; j < other.Height; j++)
                for (int i = 0; i < other.Width; i++)
                    Assert.AreEqual(one[i, j], other[j, i]);
        }


        [TestMethod]
        public void TestTransposeNaive()
        {
            Matrix<int> trans = new Matrix<int>(_m);
            trans.TransposeInternalNaive();
            AssertMatrixEqual(_m, trans);
        }

        [TestMethod]
        public void TestTransposeComplex()
        {
            Matrix<int> trans = new Matrix<int>(_m);
            trans.TransposeInternal();
            AssertMatrixEqual(_m, trans);
        }


        [TestMethod]
        public void TestTransposeTemp()
        {
            Matrix<int> trans = new Matrix<int>(_m);
            trans.TransposeAndSwapTest();
            CollectionAssert.AllItemsAreUnique(trans.Elements);
            CollectionAssert.AreEquivalent(_m.Elements, trans.Elements);
        }
    }
}
