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

            return keys.Select((k, i) =>
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
            var min = NewNode(-1);
            _heap.Add(min.Key, min.Value);

            for (int i = 0; i < Rounds; i++)
            {
                Assert.AreEqual(i + 1, _heap.Count);

                var temp = NewNode(i);
                _heap.Add(temp.Key, temp.Value);

                Assert.AreEqual(_heap.PeekMin().Key, min.Key);
                Assert.AreEqual(_heap.PeekMin().Value, min.Value);
            }
        }

        [TestMethod]
        public void TestRootRandom()
        {
            var min = AddItems(1).First().Key;

            for (int i = 0; i < Rounds; i++)
            {
                Assert.AreEqual(i + 1, _heap.Count);

                var temp = NewNode();
                _heap.Add(temp.Key, temp.Value);

                if (temp.Key <= min)
                    min = temp.Key;

                Assert.AreEqual(_heap.PeekMin().Key, min);
            }
        }

        [TestMethod]
        public void TestDelayedPeekMinRandom()
        {
            var min = AddItems(1).First().Key;

            for (int i = 0; i < Rounds; i++)
            {
                Assert.AreEqual(i + 1, _heap.Count);

                var temp = NewNode();
                _heap.Add(temp.Key, temp.Value);

                if (temp.Key <= min)
                    min = temp.Key;
            }

            Assert.AreEqual(_heap.PeekMin().Key, min);
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
                var treeItemsCollection = treeItems.ToArray();

                Assert.AreEqual(ItemCount, _heap.Count);
                Assert.AreEqual(ItemCount, treeItems.Count);
                Assert.AreEqual(ItemCount, treeItemsCollection.Length);

                Debug.WriteLine(treeItems.ToString(n => n.Key.ToString()));

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
        public void TestRemoveStraight()
        {
            for (int i = 0; i < Rounds; i++)
            {
                var items = AddItems(ItemCount);

                for (int j = 0; j < items.Length; j++)
                {
                    int expectedCount = items.Length - j;
                    Assert.AreEqual(_heap.Count, expectedCount);

                    _heap.DeleteMin();
                    Assert.AreEqual(_heap.Count, expectedCount - 1);
                }

                Assert.IsNull(_heap.PeekMin());
                Assert.AreEqual(_heap.Count, 0);
            }
        }

        [TestMethod]
        public void TestRemoveZig()
        {
            for (int i = 0; i < Rounds; i++)
            {
                var item = NewNode();
                _heap.Add(item.Key, item.Value);
                Assert.AreEqual(_heap.Count, 1);

                _heap.DeleteMin();
                Assert.AreEqual(_heap.Count, 0);
                Assert.IsNull(_heap.PeekMin());
            }
        }

        [TestMethod]
        public void TestDecreaseKey()
        {
            var items = AddItems(ItemCount);

            for (int i = 0; i < Rounds; i++)
                foreach (var heapNode in items)
                {
                    _heap.DecreaseKey(heapNode, heapNode.Key - _rnd.Next(15) - 1);
                    Assert.AreEqual(_heap.Count, items.Length);
                }
        }

        [TestMethod]
        public void TestDecreaseKeySingle()
        {
            for (int i = 0; i < Rounds; i++)
            {
                AddItems(ItemCount - 1);

                var newNode = NewNode();
                var item = _heap.Add(newNode.Key, newNode.Value);

                for (int j = 0; j < ItemCount; j++)
                {
                    _heap.DecreaseKey(item, item.Key - _rnd.Next(15) - 1);
                    Assert.AreEqual(_heap.Count, ItemCount);
                    Assert.IsTrue(item.Key < newNode.Key);
                }

                _heap.Clear();
            }
        }
    }
}
