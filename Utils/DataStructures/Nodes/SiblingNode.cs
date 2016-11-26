using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Utils.DataStructures.Nodes
{
    internal class SiblingNode<TKey, TValue>
            : DictionaryBase<TKey, TValue>.NodeItem
        where TKey : struct
        where TValue : IEquatable<TValue>
    {
        #region Fields

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
        { }

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

        #region Traversal

        public IEnumerable<SiblingNode<TKey, TValue>> GetSiblings()
        {
            var actSibling = this;
            var nextSibling = actSibling.RightSibling;

            yield return actSibling;

            while (actSibling != nextSibling)
            {
                actSibling = nextSibling;
                yield return actSibling;
                nextSibling = actSibling.RightSibling;
            }
        }

        #endregion
    }
}
