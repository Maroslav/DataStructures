namespace Utils.DataStructures.Internal
{
    // Used during traversal with a stack
    // PreAction is missing because it is always executed right away
    internal enum NodeTraversalAction
    {
        Sift,
        // Used when sifting through sibling nodes with circular references
        SiftOnlySiblings,
        InAction,
        PostAction,
    }

    internal struct NodeTraversalToken<TNode, TAction>
        where TAction : struct
    {
        public readonly TNode Node;
        public readonly TAction Action;

        public NodeTraversalToken(TNode node, TAction action)
        {
            Node = node;
            Action = action;
        }

        public override string ToString()
        {
            return Action.ToString();
        }
    }
}
