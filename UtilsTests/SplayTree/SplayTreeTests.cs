using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Utils.DataStructures.SplayTree;

namespace UtilsTests.SplayTree
{
    [TestClass]
    public class SplayTreeTests
    {
        private readonly Random _rnd = new Random(1);
        private readonly SplayTree<int, string> _tree = new SplayTree<int, string>();

        [TestMethod]
        public void TestItems()
        {
            const int itemCount = 50;
            var items = Enumerable.Range(0, itemCount).Select(n => _rnd.Next()).Select(k => new { Key = k, Value = k.ToString() });

            foreach (var item in items)
            {
                _tree.Add(item.Key, item.Value);
            }

            var treeItems = _tree.Items;

            Assert.AreEqual(itemCount, treeItems.Count);

            foreach (var item in items.OrderBy(item => item.Key)
                .Zip(treeItems, (original, new1) => new { OriginalItem = original, NewItem = new1 }))
            {
                Assert.AreEqual(item.OriginalItem.Key, item.NewItem.Key);
                Assert.AreEqual(item.OriginalItem.Value, item.NewItem.Value);
            }
        }
    }
}
