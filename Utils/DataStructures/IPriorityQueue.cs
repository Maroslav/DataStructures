using Utils.DataStructures.Nodes;

namespace Utils.DataStructures
{
    public interface IPriorityQueue<TKey, TValue>
    {
        int Count { get; }

        NodeItem<TKey, TValue> Add(TKey key, TValue val);

        NodeItem<TKey, TValue> PeekMin();

        void DecreaseKey(NodeItem<TKey, TValue> node, TKey newKey);
        void DeleteMin();
        void Delete(NodeItem<TKey, TValue> node);

        void Clear();
        void Merge(IPriorityQueue<TKey, TValue> other);
    }
}
