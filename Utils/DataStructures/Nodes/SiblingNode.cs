using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Utils.DataStructures.Nodes
{
    internal class SiblingNode<TKey, TValue>
            : NodeItem<TKey, TValue>
        where TKey : struct
        where TValue : IEquatable<TValue>
    {
        #region Fields

        // Siblings are kept with cyclic pointers (one node has itself as siblings)
        private SiblingNode<TKey, TValue> _leftSibling;
        private SiblingNode<TKey, TValue> _rightSibling;

        #endregion

        #region Properties

        public SiblingNode<TKey, TValue> LeftSibling
        {
            get { return _leftSibling; }
            set
            {
                _leftSibling = value;
                if (_leftSibling != null)
                    _leftSibling._rightSibling = this;
            }
        }

        public SiblingNode<TKey, TValue> RightSibling
        {
            get { return _rightSibling; }
            set
            {
                _rightSibling = value;
                if (_rightSibling != null)
                    _rightSibling._leftSibling = this;
            }
        }

        #endregion

        #region Genesis

        public SiblingNode(TKey key, TValue value)
            : base(key, value)
        {
            LeftSibling = this; // Sets right sibling..
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Clear()
        {
            _leftSibling = null;
            _rightSibling = null;
        }

        public override void Dispose()
        {
            Clear();
            base.Dispose();
        }

        #endregion

        #region Public methods

        public void InsertBefore(SiblingNode<TKey, TValue> newSiblings)
        {
            Debug.Assert(LeftSibling != null);
            LeftSibling.InsertAfter(newSiblings);
        }

        public void InsertAfter(SiblingNode<TKey, TValue> newSiblings)
        {
            var nextLocal = RightSibling;
            var lastTheir = newSiblings.LeftSibling;

            RightSibling = newSiblings;
            nextLocal.LeftSibling = lastTheir; // Opposite directions are set in setters
        }

        #endregion

        #region Traversal

        public IEnumerable<SiblingNode<TKey, TValue>> GetSiblings()
        {
            var actSibling = this;
            var nextSibling = actSibling.RightSibling;
            Debug.Assert(LeftSibling != null);

            yield return actSibling;

            while (nextSibling != this)
            {
                Debug.Assert(nextSibling != null);
                actSibling = nextSibling;
                yield return actSibling;
                nextSibling = actSibling.RightSibling;
            }
        }

        public IEnumerable<SiblingNode<TKey, TValue>> GetSiblingsReverse()
        {
            var actSibling = this;
            var nextSibling = actSibling.LeftSibling;
            Debug.Assert(LeftSibling != null);

            yield return actSibling;

            while (nextSibling != this)
            {
                Debug.Assert(nextSibling != null);
                actSibling = nextSibling;
                yield return actSibling;
                nextSibling = actSibling.LeftSibling;
            }
        }

        #endregion
    }
}
