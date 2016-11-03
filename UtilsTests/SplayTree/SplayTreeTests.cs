using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Utils.DataStructures.SplayTree;

using KeyType = System.Int32;
using ValueType = System.String;

namespace UtilsTests.SplayTree
{
    using TreeType = SplayTree<KeyType, ValueType>;
    using NodeType = Node<KeyType, ValueType>;


    [TestClass]
    public class SplayTreeTests
    {
        const int MaxKey = 9999;
        private const int ItemCount = 50;
        private const int Rounds = 50;

        private readonly Random _rnd = new Random(1);
        private readonly TreeType _tree = new TreeType();

        private int _uniqueIntIdx;
        readonly int[] _uniqueInts = new int[ItemCount * Rounds];


        public SplayTreeTests()
        {
            _uniqueInts.GenerateCombinationUnique(new HashSet<int>(), 0, MaxKey, _rnd);
        }


        private NodeType NewNode()
        {
            int key = _uniqueInts[_uniqueIntIdx++];
            return NewNode(key);
        }

        private NodeType NewNode(int key)
        {
            return new NodeType(key, key.ToString());
        }

        private NodeType[] AddItems(int itemCount, int countOffset = 0)
        {
            var items = Enumerable.Range(0, itemCount).Select(c => NewNode()).ToArray();

            for (int i = 0; i < items.Length; i++)
            {
                Assert.AreEqual(i + countOffset, _tree.Count);
                var item = items[i];
                _tree.Add(item.Key, item.Value);
            }

            return items;
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
                _tree.Add(temp.Key, temp.Value);

                AddItems(ItemCount, 1);

                var value = _tree[temp.Key];
                _tree.Add(temp.Key, "RANDOM_STUFF");

                Assert.AreNotEqual(value, _tree[temp.Key]);

                _tree.Clear();
            }
        }

        [TestMethod]
        public void TestRootSeq()
        {
            for (int i = 0; i < Rounds; i++)
            {
                Assert.AreEqual(i, _tree.Count);

                var temp = NewNode(99999 - i);
                _tree.Add(temp.Key, temp.Value);

                Assert.AreEqual(_tree.Root.Key, temp.Key);
                Assert.AreEqual(_tree.Root.Value, temp.Value);
            }
        }

        [TestMethod]
        public void TestRootRandom()
        {
            for (int i = 0; i < Rounds; i++)
            {
                Assert.AreEqual(i, _tree.Count);

                var temp = NewNode();
                _tree.Add(temp.Key, temp.Value);

                Assert.AreEqual(_tree.Root.Key, temp.Key);
                Assert.AreEqual(_tree.Root.Value, temp.Value);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(KeyNotFoundException))]
        public void TestIndexerGet()
        {
            var temp = NewNode();
            _tree[temp.Key] = temp.Value;

            AddItems(ItemCount, 1);

            Assert.AreEqual(temp.Value, _tree[temp.Key]);

            var throws = _tree[-1];
        }

        [TestMethod]
        public void TestClear()
        {
            for (int i = 0; i < Rounds; i++)
            {
                AddItems(ItemCount);
                Assert.AreEqual(ItemCount, _tree.Count);
                _tree.Clear();
                Assert.AreEqual(_tree.Count, 0);
            }
        }

        [TestMethod]
        public void TestFind()
        {
            for (int i = 0; i < Rounds; i++)
            {
                var items = AddItems(ItemCount);
                items.ShuffleFisherYates(_rnd);

                foreach (var item in items)
                {
                    Assert.AreEqual(item.Value, _tree[item.Key]);
                    Assert.AreEqual(item.Value, _tree.Root.Value);
                    Assert.AreEqual(item.Key, _tree.Root.Key);
                }

                _tree.Clear();
            }
        }

        [TestMethod]
        public void TestTraversal()
        {
            for (int i = 0; i < Rounds; i++)
            {
                var items = AddItems(ItemCount);

                var treeItems = _tree.Items;
                Debug.WriteLine(treeItems.ToString(n => n.Key.ToString()));

                var orderedItems = items.OrderBy(item => item.Key).ToArray();
                Debug.WriteLine(new TreeType.ItemCollection<NodeType>(orderedItems, ItemCount).ToString(n => n.Key.ToString()));

                CollectionAssert.AllItemsAreNotNull(orderedItems);
                CollectionAssert.AreEquivalent(orderedItems, items);

                _tree.Clear();
            }
        }

        [TestMethod]
        public void TestUnique()
        {
            for (int i = 0; i < Rounds; i++)
            {
                AddItems(ItemCount);

                var treeItems = _tree.Items;
                TreeType.NodeItem[] itemCollection = new TreeType.NodeItem[ItemCount];
                treeItems.CopyTo(itemCollection, 0);

                CollectionAssert.AllItemsAreUnique(itemCollection);

                _tree.Clear();
            }
        }

        [TestMethod]
        public void TestAbsorb()
        {
            for (int i = 0; i < Rounds; i++)
            {
                for (int j = 0; j < ItemCount; j++)
                    _tree.Add(1, j.ToString());

                var treeItems = _tree.Items;
                Assert.AreEqual(treeItems.Count, 1);

                _tree.Clear();
            }
        }

        [TestMethod]
        public void TestRemove()
        {

        }
    }
}
