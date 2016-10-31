using System;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NodeType = Utils.DataStructures.SplayTree.Node<int, string>;

namespace UtilsTests.SplayTree
{
    [TestClass]
    public class NodeTests
    {
        private Random _rnd;
        private const int MaxRand = 9999;
        private int uid = 0;

        public NodeTests()
        {
            ResetRandom();
        }

        private void ResetRandom()
        {
            _rnd = new Random(2);
        }


        private NodeType GetNewRandomNode()
        {
            int val = uid++;
            //int val = _rnd.Next(MaxRand);
            return new NodeType(val, val.ToString());
        }

        private void AddRandomChildren<T>(NodeType root, int leftMin, int rightMax)
            where T : NodeType.FlipBase<T>
        {
            var left = GetNewRandomNode();
            //left.Key = _rnd.Next(root.Key - leftMin) + leftMin;
            root.SetLeftChild<T>(left);
            Assert.AreEqual(left.Parent, root);

            var right = GetNewRandomNode();
            //right.Key = _rnd.Next(Math.Min(rightMax, MaxRand) - root.Key) + root.Key;
            root.SetRightChild<T>(right);
            Assert.AreEqual(right.Parent, root);
        }

        private void AssertFamilyEqual<T>(NodeType node, NodeType parent, NodeType leftChild, NodeType rightChild)
            where T : NodeType.FlipBase<T>
        {
            Assert.AreEqual(node.Parent, parent);
            Assert.AreEqual(node.GetLeftChild<T>(), leftChild);
            Assert.AreEqual(node.GetRightChild<T>(), rightChild);
        }


        [TestMethod]
        public void TestChildren()
        {
            var root = GetNewRandomNode();
            var temp = GetNewRandomNode();

            root.LeftChild = temp;
            root.RightChild = temp;

            Assert.AreEqual(temp.Parent, root);

            Assert.AreEqual(root.LeftChild, temp);
            Assert.AreEqual(root.RightChild, temp);
        }


        [TestMethod]
        public void TestZig()
        {
            TestZig<NodeType.NoFlip>();
            ResetRandom();
            TestZig<NodeType.DoFlip>();
        }

        private void TestZig<T>()
            where T : NodeType.FlipBase<T>
        {
            var parent = GetNewRandomNode();

            var root = GetNewRandomNode();
            parent.LeftChild = root; // Non-generic variety -- we test both cases like this
            AddRandomChildren<T>(root, 0, int.MaxValue);

            var left = root.GetLeftChild<T>();
            AddRandomChildren<T>(left, 0, root.Key - 1);

            var leftLeft = left.GetLeftChild<T>();
            var leftRight = left.GetRightChild<T>();

            var right = root.GetRightChild<T>();

            Debug.WriteLine("Pre:");
            Debug.Write(parent);

            left.Zig();

            Debug.WriteLine("Post:");
            Debug.Write(parent);

            // Left should be the root
            Assert.IsTrue(left.IsLeftChild());
            AssertFamilyEqual<T>(left, parent, leftLeft, root);
            AssertFamilyEqual<T>(root, left, leftRight, right);
        }


        [TestMethod]
        public void TestZigZag()
        {
            TestZigZag<NodeType.NoFlip>();
            ResetRandom();
            TestZigZag<NodeType.DoFlip>();
        }

        private void TestZigZag<T>()
            where T : NodeType.FlipBase<T>
        {
            var parent = GetNewRandomNode();

            var root = GetNewRandomNode();
            parent.LeftChild = root; // Non-generic variety -- we test both cases like this
            AddRandomChildren<T>(root, 0, int.MaxValue);

            var left = root.GetLeftChild<T>();
            AddRandomChildren<T>(left, 0, root.Key - 1);

            var leftLeft = left.GetLeftChild<T>();
            var leftRight = left.GetRightChild<T>();

            AddRandomChildren<T>(leftRight, left.Key, root.Key);
            var leftRightLeft = leftRight.GetLeftChild<T>();
            var leftRightRight = leftRight.GetRightChild<T>();

            var right = root.GetRightChild<T>();

            Debug.WriteLine("Pre:");
            Debug.Write(parent);

            leftRight.ZigZxg();

            Debug.WriteLine("Post:");
            Debug.Write(parent);

            // LeftRight should be the root
            Assert.IsTrue(leftRight.IsLeftChild());
            AssertFamilyEqual<T>(leftRight, parent, left, root);
            // Left stays the left child of new root (now LeftRight)
            AssertFamilyEqual<T>(left, leftRight, leftLeft, leftRightLeft);
            // Root should be the right child of new root
            AssertFamilyEqual<T>(root, leftRight, leftRightRight, right);

            leftRight.Zig();

            Debug.WriteLine("XX:");
            Debug.Write(leftRight);

            Assert.AreEqual(leftRight.Parent, null);
        }


        [TestMethod]
        public void TestZigZig()
        {
            TestZigZig<NodeType.NoFlip>();
            ResetRandom();
            TestZigZig<NodeType.DoFlip>();
        }

        private void TestZigZig<T>()
            where T : NodeType.FlipBase<T>
        {
            var parent = GetNewRandomNode();

            var root = GetNewRandomNode();
            parent.LeftChild = root; // Non-generic variety -- we test both cases like this
            AddRandomChildren<T>(root, 0, int.MaxValue);

            var left = root.GetLeftChild<T>();
            AddRandomChildren<T>(left, 0, root.Key - 1);

            var leftLeft = left.GetLeftChild<T>();
            var leftRight = left.GetRightChild<T>();

            AddRandomChildren<T>(leftLeft, 0, left.Key);
            var leftLeftLeft = leftLeft.GetLeftChild<T>();
            var leftLeftRight = leftLeft.GetRightChild<T>();

            var right = root.GetRightChild<T>();

            Debug.WriteLine("Pre:");
            Debug.Write(parent);

            leftLeft.ZigZxg();

            Debug.WriteLine("Post:");
            Debug.Write(parent);

            // LeftRight should be the root
            Assert.IsTrue(leftLeft.IsLeftChild());
            AssertFamilyEqual<T>(leftLeft, parent, leftLeftLeft, left);
            // Left should be the right child of the new root (now leftLeft)
            AssertFamilyEqual<T>(left, leftLeft, leftLeftRight, root);
            // Root should be the right child of the right child of the new root
            AssertFamilyEqual<T>(root, left, leftRight, right);

            leftLeft.Zig();

            Debug.WriteLine("XX:");
            Debug.Write(leftLeft);

            Assert.AreEqual(leftLeft.Parent, null);
        }
    }
}
