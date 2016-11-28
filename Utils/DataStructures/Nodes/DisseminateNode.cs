using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Utils.DataStructures.Internal;

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

        public int ChildrenCount { get; protected set; }

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
            ChildrenCount++;

            if (FirstChild == null)
            {
                FirstChild = child;
                return;
            }

            // Always add it as the last child
            FirstChild.InsertBefore(child);
        }

        /// <summary>
        /// Gracefully remove this node from the parent -- connect neighbouring siblings and update the parent.
        /// Children are preserved.
        /// </summary>
        public void CutFromFamily()
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
                LeftSibling = this;
                RightSibling = this;
            }
        }

        #endregion

        #region Traversal

        /// <summary>
        /// Traverses the graph starting with each nodes children, followed by siblings.
        /// </summary>
        public bool Sift(NodeTraversalActions<TKey, TValue, DisseminateNode<TKey, TValue>, NodeTraversalAction> nodeActions)
        {
            // We have to use an iterative way because the default stack size of .net apps is 1MB
            // and it's impractical to change it.....
            var stack = nodeActions.TraversalStack;
            Debug.Assert(stack.Count == 0);
            stack.Push(GetNodeTraversalToken(this, NodeTraversalAction.Sift));

            try
            {
                while (stack.Count > 0)
                {
                    var token = stack.Pop();

                    switch (token.Action)
                    {
                        case NodeTraversalAction.Sift:
                        case NodeTraversalAction.SiftIgnoreSiblings:
                            if (!token.Node.HandleSift(stack, nodeActions, token.Action != NodeTraversalAction.SiftIgnoreSiblings))
                                return false;
                            break;

                        case NodeTraversalAction.PostAction:
                            if (!nodeActions.InvokePostAction(token.Node))
                                return false;
                            break;

                        default: // No inAction here
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
            finally
            {
                stack.Clear();
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool HandleSift(
            Stack<NodeTraversalToken<DisseminateNode<TKey, TValue>, NodeTraversalAction>> stack,
            NodeTraversalActions<TKey, TValue, DisseminateNode<TKey, TValue>, NodeTraversalAction> nodeActions,
            bool addSiblings)
        {
            // First and only visit to this node
            if (!nodeActions.InvokePreAction(this))
                return false;


            // Push actions in reverse order
            // Push all siblings
            if (addSiblings && RightSibling != this)
                foreach (var siblingNode in GetSiblingsReverse().Where(s => s != this))
                    // Notify that when being sifted, don't try to add all siblings again
                    stack.Push(GetNodeTraversalToken((DisseminateNode<TKey, TValue>)siblingNode, NodeTraversalAction.SiftIgnoreSiblings));


            // Push the child
            if (FirstChild != null)
            {
                if (nodeActions.HasPostAction)
                    stack.Push(GetNodeTraversalToken(this, NodeTraversalAction.PostAction));

                stack.Push(GetNodeTraversalToken(FirstChild, NodeTraversalAction.Sift));
            }
            else if (!nodeActions.InvokePostAction(this)) // Handle missing children -- we can invoke actions right away
                return false;

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private NodeTraversalToken<DisseminateNode<TKey, TValue>, NodeTraversalAction> GetNodeTraversalToken(DisseminateNode<TKey, TValue> node, NodeTraversalAction action)
        {
            return new NodeTraversalToken<DisseminateNode<TKey, TValue>, NodeTraversalAction>(node, action);
        }

        #endregion
    }
}
