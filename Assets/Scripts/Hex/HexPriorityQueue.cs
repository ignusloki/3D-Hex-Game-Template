using System.Collections.Generic;

public sealed class HexPriorityQueue<T>
{
    private readonly List<(T Item, float Priority)> heap = new();

    public int Count => heap.Count;

    public void Enqueue(T item, float priority)
    {
        heap.Add((item, priority));
        HeapifyUp(heap.Count - 1);
    }

    public (T Item, float Priority) Dequeue()
    {
        (T Item, float Priority) root = heap[0];
        int lastIndex = heap.Count - 1;
        heap[0] = heap[lastIndex];
        heap.RemoveAt(lastIndex);

        if (heap.Count > 0)
        {
            HeapifyDown(0);
        }

        return root;
    }

    private void HeapifyUp(int index)
    {
        while (index > 0)
        {
            int parentIndex = (index - 1) / 2;
            if (heap[index].Priority >= heap[parentIndex].Priority)
            {
                break;
            }

            (heap[index], heap[parentIndex]) = (heap[parentIndex], heap[index]);
            index = parentIndex;
        }
    }

    private void HeapifyDown(int index)
    {
        while (true)
        {
            int leftChild = (index * 2) + 1;
            int rightChild = leftChild + 1;
            int smallest = index;

            if (leftChild < heap.Count && heap[leftChild].Priority < heap[smallest].Priority)
            {
                smallest = leftChild;
            }

            if (rightChild < heap.Count && heap[rightChild].Priority < heap[smallest].Priority)
            {
                smallest = rightChild;
            }

            if (smallest == index)
            {
                break;
            }

            (heap[index], heap[smallest]) = (heap[smallest], heap[index]);
            index = smallest;
        }
    }
}
