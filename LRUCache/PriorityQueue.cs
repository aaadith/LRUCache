using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LRUCache
{
    public interface IPriorityQueue<T>
    {
        void Add(T data);
        void Remove(T data);
        void DeleteStrongestCandidate();
        T GetStrongestCandidate();
        void Heapify();
        int GetCount();
    }
    public class PriorityQueue<T> : IPriorityQueue<T> where T : class,IComparable
    {

        List<T> list;

        public PriorityQueue()
        {
            list = new List<T>();
        }

        public void Add(T data)
        {
            list.Add(data);            
        }

        public void Remove(T data)
        {
            if (list[0].Equals(data))
            {
                list.Remove(data);
                Heapify();
            }
            else
            {
                list.Remove(data);
            }
            
        }

        public void Heapify()
        {
            
            for (int i = list.Count/2; i > 0; i--)
            {
                int left = i * 2 - 1, right = i * 2, current = i - 1;

                int max;
                if (right > list.Count)
                    max = left;
                else
                    max = (list[left].CompareTo(list[right]) < 0) ? left : right;

                if (list[current].CompareTo(list[max]) < 0)
                    Swap(current, max);
                    
            }
        }

        private void Swap(int i, int j)
        {
            T temp = null;
            temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }

        public void DeleteRoot()
        {
            if (list.Count > 0)
            {
                list[0] = list[list.Count - 1];
                list.RemoveAt(list.Count - 1);
                Heapify();
            }
        }


        public int GetCount()
        {
            throw new NotImplementedException();
        }


        public void DeleteStrongestCandidate()
        {
            throw new NotImplementedException();
        }

        public T GetStrongestCandidate()
        {
            throw new NotImplementedException();
        }
    }
}
