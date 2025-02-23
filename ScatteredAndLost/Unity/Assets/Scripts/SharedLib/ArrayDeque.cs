using System;
using System.Collections;
using System.Collections.Generic;

namespace HK8YPlando.Scripts.SharedLib
{
    internal class ArrayDequeIterator<T> : IEnumerator<T>
    {
        private readonly ArrayDeque<T> queue;
        private int? index;

        public ArrayDequeIterator(ArrayDeque<T> queue) => this.queue = queue;

        public T Current => queue[index.Value];

        object IEnumerator.Current => queue[index.Value];

        public void Dispose() { }

        public bool MoveNext()
        {
            index = (index ?? -1) + 1;
            return index < queue.Count;
        }

        public void Reset() => index = null;
    }

    public class ArrayDeque<T> : IEnumerable<T>
    {
        private List<T> elements = new List<T>(10);
        private int start;
        private int size;

        public ArrayDeque()
        {
            for (int i = 0; i < 10; i++) elements.Add(default);
        }

        public int Count => size;

        public T this[int index]
        {
            get { return Get(index); }
            set { Set(index, value); }
        }

        public T Get(int index)
        {
            if (index < 0 || index >= size) throw new IndexOutOfRangeException($"{index} not in [0, {size})");
            return elements[(index + start) % elements.Count];
        }

        public void Set(int index, T value)
        {
            if (index < 0 || index >= size) throw new IndexOutOfRangeException($"{index} not in [0, {size})");
            elements[(index + start) % elements.Count] = value;
        }

        public T First() => Get(0);

        public T Last() => Get(size - 1);

        public void AddLast(T elem)
        {
            if (elements.Count == size)
            {
                var newElements = new List<T>(size * 2);
                for (int i = 0; i < size * 2; i++) newElements.Add(i < size ? Get(i) : default);
                elements = newElements;
                start = 0;
            }

            elements[(start + size++) % elements.Count] = elem;
        }

        public void RemoveFirst(int num = 1)
        {
            if (num < 0) throw new IndexOutOfRangeException($"{num} < 0");
            if (num > size) throw new IndexOutOfRangeException($"{num} > {size}");

            start = (start + num) % elements.Count;
            size -= num;
        }

        public IEnumerator<T> GetEnumerator() => new ArrayDequeIterator<T>(this);

        IEnumerator IEnumerable.GetEnumerator() => new ArrayDequeIterator<T>(this);
    }

    // An array deque which maintains permanent indices for items added.
    internal class HistoryWindow<T>
    {
        private int removed = 0;
        private ArrayDeque<T> queue = new ArrayDeque<T>();

        public int Count => removed + queue.Count;

        public int Removed => removed;

        public T Get(int index) => queue.Get(index - removed);

        public T GetFirstAvailable() => queue.Get(0);

        public T Last() => queue.Last();

        public void AddLast(T elem) => queue.AddLast(elem);

        public IEnumerable<T> AvailableHistory() => queue;

        public void RemoveBefore(int idx)
        {
            if (idx < removed) throw new IndexOutOfRangeException($"{idx} < {removed}");
            if (idx == removed) return;

            queue.RemoveFirst(idx - removed);
            removed = idx;
        }
    }

    // History window with permanently increasing sorted keys, and associated values.
    internal class IndexedHistoryWindow<K, V> where K : IComparable<K>
    {
        private HistoryWindow<(K, V)> tuples = new HistoryWindow<(K, V)>();

        // Gets the value associated with the largest key <= the argument.
        public V LowerBound(K key) => tuples.Get(LowerBoundIndex(key)).Item2;

        private int LowerBoundIndex(K key)
        {
            K k1 = tuples.GetFirstAvailable().Item1;
            K k2 = tuples.Last().Item1;
            if (key.CompareTo(k2) >= 0) return tuples.Count - 1;
            if (key.CompareTo(k1) <= 0) return tuples.Removed;

            int low = tuples.Removed;
            int high = tuples.Count - 1;
            while (high - low > 1)
            {
                int mid = (low + high) / 2;
                (K k, V v) = tuples.Get(mid);
                int compare = key.CompareTo(k);
                if (compare == 0) return mid;
                else if (compare < 0) high = mid;
                else low = mid;
            }

            return low;
        }

        public void AddLast(K key, V value)
        {
            if (tuples.Count > 0 && tuples.Last().Item1.CompareTo(key) > 0) throw new ArgumentException($"Key ({key}) is less than the last ({tuples.Last().Item1})");
            tuples.AddLast((key, value));
        }

        public void RemoveBefore(K key) => tuples.RemoveBefore(LowerBoundIndex(key));
    }
}