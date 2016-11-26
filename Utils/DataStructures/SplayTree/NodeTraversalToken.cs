namespace Utils.DataStructures.Internal
{
    internal struct NodeTraversalToken<TNode, TAction>
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
