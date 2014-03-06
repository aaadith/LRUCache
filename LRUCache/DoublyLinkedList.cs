/*using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LRUCache
{
    public class DoublyLinkedListNode<T>
    {
        T data;
        public DoublyLinkedListNode<T> previous, next;

        public DoublyLinkedListNode(T data)
        {
            this.data = data;
        }        
    }

    public class DoublyLinkedList<T>
    {
        DoublyLinkedListNode<T> head,tail;

        public DoublyLinkedList()
        {
            head = null;
            tail = null;
        }

        public void AddToHead(T data)
        {
            DoublyLinkedListNode<T> newnode = new DoublyLinkedListNode<T>(data);
            head.previous = newnode;
            newnode.next = head;

            lock (head)
            {
                head = newnode;
            }

            if (tail == null)
            {
                lock (tail)
                {
                    tail = head;
                }
            }
        }

        public void Delete(DoublyLinkedListNode<T> node)
        {
            if (tail == node)
            {
                lock (tail)
                {
                    tail = tail.previous;
                }
            }

            if (head == node)
            {
                lock (head)
                {
                    head = head.next;
                }
            }

            if (node.previous != null)
            {
                DoublyLinkedListNode<T> previous = node.previous;
                lock (previous)
                {
                    previous.next = node.next;
                }
            }
            if (node.next != null)
            {
                DoublyLinkedListNode<T> next = node.next;
                lock (next)
                {
                    node.next.previous = node.previous;
                }
            }
        }

        public T Get(DoublyLinkedListNode<T> node)
        {
            node.previous.next = node.next;
            node.next.previous = node.previous;

            lock (head)
            {
                node.next = head;
                head = node;
            }

            //return node.
        }

    }
}

*/