using System;
using System.Diagnostics;
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
                LeftSibling = null;
                RightSibling = null;
            }
        }

        #endregion

        #region Traversal

        /// <summary>
        /// Traverses the graph starting with each nodes children, followed by siblings.
        /// </summary>
        private bool Sift(NodeTraversalActions<TKey, TValue, DisseminateNode<TKey, TValue>, BinaryNodeAction> nodeActions)
        {
            // We have to use an iterative way because the default stack size of .net apps is 1MB
            // and it's impractical to change it.....
            var stack = nodeActions.TraversalStack;
            Debug.Assert(stack.Count == 0);
            stack.Push(GetNodeTraversalToken(this, BinaryNodeAction.Sift));

            try
            {
                while (stack.Count > 0)
                {
                    var token = stack.Pop();

                    switch (token.Action)
                    {
                        case BinaryNodeAction.Sift:
                            if (!token.Node.HandleSift(stack, nodeActions))
                                return false;
                            break;

                        case BinaryNodeAction.InAction:
                            if (!nodeActions.InvokeInAction(token.Node))
                                return false;
                            break;

                        case BinaryNodeAction.PostAction:
                            if (!nodeActions.InvokePostAction(token.Node))
                                return false;
                            break;

                        default:
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
        private bool HandleSift(Stack<NodeTraversalToken<DisseminateNode<TKey, TValue>, BinaryNodeAction>> stack, NodeTraversalActions<TKey, TValue, DisseminateNode<TKey, TValue>, BinaryNodeAction> nodeActions)
        {
            // First and only visit to this node
            if (!nodeActions.InvokePreAction(this))
                return false;


            // Push actions in reverse order
            var right = RightSibling;
            var left = LeftSibling;

            if (right != null)
            {
                stack.Push(GetNodeTraversalToken(this, BinaryNodeAction.PostAction));
                stack.Push(GetNodeTraversalToken(right, BinaryNodeAction.Sift));
            }
            else if (left != null)
                // We need to store the action (it has to be executed after sifting through Left)
                stack.Push(GetNodeTraversalToken(this, BinaryNodeAction.PostAction));

            if (left != null)
            {
                stack.Push(GetNodeTraversalToken(this, BinaryNodeAction.InAction));
                stack.Push(GetNodeTraversalToken(left, BinaryNodeAction.Sift));
            }
            else
            {
                // Handle missing children -- we can only invoke actions right away if children are null from left to right
                if (!nodeActions.InvokeInAction(this))
                    return false;

                if (right == null)
                    if (!nodeActions.InvokePostAction(this))
                        return false;
            }

            return true;
        }

        private NodeTraversalToken<DisseminateNode<TKey, TValue>, BinaryNodeAction> GetNodeTraversalToken(DisseminateNode<TKey, TValue> node, BinaryNodeAction action)
        {
            return new NodeTraversalToken<DisseminateNode<TKey, TValue>, BinaryNodeAction>(node, action);
        }

        #endregion
    }
}
