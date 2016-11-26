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
        internal DisseminateNode<TKey, TValue> FirstChild;

        #endregion

        #region Properties

        public int ChildrenCount { get; private set; }

        private new DisseminateNode<TKey, TValue> LeftSibling
        {
            get { return (DisseminateNode<TKey, TValue>)base.LeftSibling; }
            set { base.LeftSibling = value; }
        }

        private new DisseminateNode<TKey, TValue> RightSibling
        {
            get { return (DisseminateNode<TKey, TValue>)base.RightSibling; }
            set { base.RightSibling = value; }
        }

        #endregion

        #region Genesis

        public DisseminateNode(TKey key, TValue value)
            : base(key, value)
        { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Clear()
        {
            Parent = null;
            FirstChild = null;
        }

        public override void Dispose()
        {
            Clear();
            base.Dispose();
        }

        #endregion

        #region Public methods

        public void AddChild(DisseminateNode<TKey, TValue> child)
        {

            child.Parent = this;
        }

        /// <summary>
        /// Gracefully remove this node from the parent -- connect neighbouring siblings and update the parent.
        /// </summary>
        public void CutFromParent()
        {
            LeftSibling.RightSibling = RightSibling; // The other direction is updated automagically

            try
            {
                if (Parent == null)
                    return;

                // Update the parent
                if (Parent.ChildrenCount == 1)
                {
                    // We are the only child
                    Debug.Assert(LeftSibling == RightSibling && RightSibling == this);
                    Parent.FirstChild = null;
                    Parent.ChildrenCount = 0;
                    return;
                }

                if (Parent.FirstChild == this)
                    Parent.FirstChild = RightSibling;

                Parent.ChildrenCount--;
            }
            finally
            {
                Parent = null;
                LeftSibling = null;
                RightSibling = null;
            }
        }

        #endregion

        #region Traversal

        #endregion
    }
}
