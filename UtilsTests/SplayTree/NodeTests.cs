using System;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NodeType = Utils.DataStructures.Nodes.BinaryNode<int, string>;

namespace UtilsTests.SplayTree
{
    [TestClass]
    public class NodeTests
    {
        private int _uid = 0;


        private NodeType GetNewRandomNode()
        {
            int val = _uid++;
            return new NodeType(val, val.ToString());
        }

        private void AddRandomChildren<T>(NodeType root)
            where T : NodeType.FlipBase<T>
        {
            var left = GetNewRandomNode();
            root.SetLeftChild<T>(left);
            Assert.AreEqual(left.Parent, root);

            var right = GetNewRandomNode();
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
            var temp1 = GetNewRandomNode();
            var temp2 = GetNewRandomNode();

            root.LeftChild = temp1;
            root.RightChild = temp2;

            Debug.WriteLine(root);

            Assert.AreEqual(temp1.Parent, root);
            Assert.AreEqual(temp2.Parent, root);

            Assert.AreEqual(root.LeftChild, temp1);
            Assert.AreEqual(root.RightChild, temp2);
        }


        [TestMethod]
        public void TestZig()
        {
            TestZig<NodeType.NoFlip>();
            TestZig<NodeType.DoFlip>();
        }

        private void TestZig<T>()
            where T : NodeType.FlipBase<T>
        {
            var parent = GetNewRandomNode();

            var root = GetNewRandomNode();
            parent.LeftChild = root; // Non-generic variety -- we test both cases like this
            AddRandomChildren<T>(root);

            var left = root.GetLeftChild<T>();
            AddRandomChildren<T>(left);

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
            TestZigZag<NodeType.DoFlip>();
        }

        private void TestZigZag<T>()
            where T : NodeType.FlipBase<T>
        {
            var parent = GetNewRandomNode();

            var root = GetNewRandomNode();
            parent.LeftChild = root; // Non-generic variety -- we test both cases like this
            AddRandomChildren<T>(root);

            var left = root.GetLeftChild<T>();
            AddRandomChildren<T>(left);

            var leftLeft = left.GetLeftChild<T>();
            var leftRight = left.GetRightChild<T>();

            AddRandomChildren<T>(leftRight);
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

            Debug.WriteLine("Zig:");
            Debug.Write(leftRight);

            Assert.AreEqual(leftRight.Parent, null);
        }


        [TestMethod]
        public void TestZigZig()
        {
            TestZigZig<NodeType.NoFlip>();
            TestZigZig<NodeType.DoFlip>();
        }

        private void TestZigZig<T>()
            where T : NodeType.FlipBase<T>
        {
            var parent = GetNewRandomNode();

            var root = GetNewRandomNode();
            parent.LeftChild = root; // Non-generic variety -- we test both cases like this
            AddRandomChildren<T>(root);

            var left = root.GetLeftChild<T>();
            AddRandomChildren<T>(left);

            var leftLeft = left.GetLeftChild<T>();
            var leftRight = left.GetRightChild<T>();

            AddRandomChildren<T>(leftLeft);
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

            Debug.WriteLine("Zig:");
            Debug.Write(leftLeft);

            Assert.AreEqual(leftLeft.Parent, null);
        }
    }
}
