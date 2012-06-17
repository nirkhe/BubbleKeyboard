using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace PointAndClick
{
    class RotationalArray<T> : ICollection<T>
    {
        readonly int size;
        private int index;
        private T[] container;

        public int Count
        {
            get { return size; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public RotationalArray(int size, int startIndex)
        {
            this.size = size;
            container = new T[size];

            if (startIndex >= 0 && startIndex < size)
            {
                this.index = startIndex;
            }
            else
            {
                throw new ArgumentOutOfRangeException("startIndex", "startIndex must be greater than zero and less than size");
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return container.GetEnumerator();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return (IEnumerator<T>) container.GetEnumerator();
        }

        public bool Contains(T item)
        {
            return container.Contains<T>(item);
        }

        public void Clear()
        {
            for (int i = container.Length - 1; i >= 0; i--)
            {
                container[i] = default(T);
            }
            return;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            container.CopyTo(array, arrayIndex);
        }

        public void Add(T item)
        {
            if (index >= size)
            {
                index = 0;
            }
            container[index] = item;
            index++;
        }

        public bool Remove(T item)
        {
            throw new NotSupportedException("Elements in Rotational Array cannot be removed by reference");
        }

        public T GetFirst()
        {
            if (index >= size)
            {
                return container[0];
            }
            else
            {
                return container[index];
            }
        }

        public T GetLast()
        {
            if (index <= 0)
            {
                return container[size - 1];
            }
            else
            {
                return container[index - 1];
            }
        }
    }
}
