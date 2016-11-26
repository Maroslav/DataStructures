using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Utils.DataStructures.Nodes
{
    internal class DisseminateNode<TKey, TValue>
            : SiblingNode<TKey, TValue>
        where TKey : struct
        where TValue : IEquatable<TValue>
    {
        #region Fields

        public DisseminateNode<TKey, TValue> Parent;

        // Children are kept with cyclic pointers (one child has itself as siblings)
        // We store the left-most child; to reach the last child, we can get the left sibling of the child
        private DisseminateNode<TKey, TValue> _children;

        #endregion

        #region Properties

        public int ChildrenCount { get; private set; }
        
        #endregion

        #region Genesis

        public DisseminateNode(TKey key, TValue value)
            : base(key, value)
        { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Clear()
        {
            Parent = null;
            _children = null;
        }

        public override void Dispose()
        {
            Clear();
            base.Dispose();
        }

        #endregion

        #region Helpers

        public void AddChild(DisseminateNode<TKey, TValue> child)
        {

            child.Parent = this;
        }

        #endregion

        #region Traversal
        
        #endregion
    }
}
