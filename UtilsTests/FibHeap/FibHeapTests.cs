using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Utils.DataStructures;
using Utils.DataStructures.Nodes;

using KeyType = System.Int32;
using ValueType = System.String;

namespace UtilsTests.FibHeap
{
    using HeapType = FibonacciHeap<KeyType, ValueType>;
    using NodeType = FibonacciHeap<KeyType, ValueType>.HeapNode;


    [TestClass]
    public class FibHeapTests
    {
        const int MaxKey = 9999;
        private const int ItemCount = 50;
        private const int Rounds = 50;

        private readonly Random _rnd = new Random(1);
        private readonly HeapType _heap = new HeapType();

        private int _uniqueIntIdx;
        readonly int[] _uniqueKeys = new int[ItemCount * Rounds];


        public FibHeapTests()
        {
            _uniqueKeys.GenerateCombinationUnique(new HashSet<int>(), 0, MaxKey, _rnd);
        }


        private KeyType NewKey()
        {
            var key = _uniqueKeys[_uniqueIntIdx++];
            return key;
        }

        private NodeType NewNode()
        {
            KeyType key = NewKey();
            return new NodeType(key, key.ToString());
        }

        private NodeType NewNode(int key)
        {
            return new NodeType(key, key.ToString());
        }

        private NodeType[] AddItems(int itemCount, int countOffset = 0)
        {
            var keys = Enumerable.Range(0, itemCount).Select(c => NewKey()).ToArray();

            return keys.Select((i, k) =>
            {
                Assert.AreEqual(i + countOffset, _heap.Count);
                return (NodeType)_heap.Add(k, k.ToString());
            }).ToArray();
        }


        [TestMethod]
        public void TestEmpty()
        {
            Assert.IsNull(_heap.PeekMin());
        }

        [TestMethod]
        public void TestAddEmpty()
        {
            AddItems(1);
            _heap.DeleteMin();
            Assert.IsNull(_heap.PeekMin());
        }

        [TestMethod]
        public void TestAdd()
        {
            for (int i = 0; i < Rounds; i++)
                AddItems(ItemCount, i * ItemCount);
        }

        [TestMethod]
        public void TestRepeatedAdd()
        {
            for (int i = 0; i < Rounds; i++)
            {
                var temp = NewNode(-1);
                _heap.Add(temp.Key, temp.Value);
                AddItems(ItemCount, 1);
                _heap.Add(temp.Key, temp.Value + 'a');

                Assert.IsTrue(_heap.PeekMin().Value == temp.Value || _heap.PeekMin().Value == temp.Value + 'a');

                _heap.Clear();
            }
        }

        [TestMethod]
        public void TestRootSeqDown()
        {
            for (int i = 0; i < Rounds; i++)
            {
                Assert.AreEqual(i, _heap.Count);

                var temp = NewNode(99999 - i);
                _heap.Add(temp.Key, temp.Value);

                Assert.AreEqual(_heap.PeekMin().Key, temp.Key);
                Assert.AreEqual(_heap.PeekMin().Value, temp.Value);
            }
        }

        [TestMethod]
        public void TestRootSeqUp()
        {
            var min = AddItems(1).First();

            for (int i = 0; i < Rounds; i++)
            {
                Assert.AreEqual(i, _heap.Count);

                var temp = NewNode(i);
                _heap.Add(temp.Key, temp.Value);

                Assert.AreEqual(_heap.PeekMin().Key, min.Key);
                Assert.AreEqual(_heap.PeekMin().Value, min.Value);
            }
        }

        [TestMethod]
        public void TestRootRandom()
        {
            var min = AddItems(1).First();

            for (int i = 0; i < Rounds; i++)
            {
                Assert.AreEqual(i, _heap.Count);

                var temp = NewNode();
                _heap.Add(temp.Key, temp.Value);

                Assert.AreEqual(_heap.PeekMin().Key, min.Key);
                Assert.AreEqual(_heap.PeekMin().Value, min.Value);
            }
        }

        [TestMethod]
        public void TestClear()
        {
            for (int i = 0; i < Rounds; i++)
            {
                AddItems(ItemCount);
                Assert.AreEqual(ItemCount, _heap.Count);
                _heap.Clear();
                Assert.AreEqual(_heap.Count, 0);

                Assert.IsTrue(_heap.Items.Count == 0);
                Assert.IsTrue(!_heap.Items.Any());
            }
        }

        [TestMethod]
        public void TestTraversal()
        {
            for (int i = 0; i < Rounds; i++)
            {
                var items = AddItems(ItemCount);

                var treeItems = _heap.Items;
                Debug.WriteLine(treeItems.ToString(n => n.Key.ToString()));
                var treeItemsCollection = treeItems.ToArray();

                CollectionAssert.AllItemsAreNotNull(treeItemsCollection);
                CollectionAssert.IsSubsetOf(treeItemsCollection, items);
                CollectionAssert.IsSubsetOf(items, treeItemsCollection);

                _heap.Clear();
            }
        }

        [TestMethod]
        public void TestUnique()
        {
            for (int i = 0; i < Rounds; i++)
            {
                AddItems(ItemCount);

                var treeItems = _heap.Items;
                NodeType[] itemCollection = new NodeType[ItemCount];
                treeItems.CopyTo(itemCollection, 0);

                CollectionAssert.AllItemsAreUnique(itemCollection);

                _heap.Clear();
            }
        }

        [TestMethod]
        public void TestRemove()
        {

        }
    }
}
