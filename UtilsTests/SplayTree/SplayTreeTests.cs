using System;
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
        private readonly Random _rnd = new Random(1);
        private readonly TreeType _tree = new TreeType();


        [TestMethod]
        public void TestItems()
        {
            const int itemCount = 50;
            const int maxKey = 9999;
            var items = Enumerable.Range(0, itemCount).Select(n => _rnd.Next(maxKey)).Select(k => new NodeType(k, k.ToString())).ToList();

            foreach (var item in items)
                _tree.Add(item.Key, item.Value);

            Debug.WriteLine(_tree);


            var treeItems = _tree.Items;
            Debug.WriteLine(treeItems.ToString(n => n.Key.ToString()));

            var orderedItems = items.OrderBy(item => item.Key);
            Debug.WriteLine(new TreeType.ItemCollection<NodeType>(orderedItems, itemCount).ToString(n => n.Key.ToString()));


            Assert.AreEqual(itemCount, treeItems.Count);

            foreach (var item in orderedItems
                .Zip(treeItems, (original, new1) => new { OriginalItem = original, NewItem = new1 }))
            {
                Assert.AreEqual(item.OriginalItem.Key, item.NewItem.Key);
                Assert.AreEqual(item.OriginalItem.Value, item.NewItem.Value);
            }
        }

        [TestMethod]
        public void TestRemove()
        {

        }
    }
}
