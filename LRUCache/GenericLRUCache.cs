using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LRUCache
{
    /*
     * Class ContentReference : 
     * used to maintain reference to data stored in cache. When a node becomes eviction 
     * candidate, reference its content is made weak so that it  becomes available for 
     * garbage collection. Space occupied by the contents would be reclaimed should the need arise.
     */
    class ContentReference<T> where T : class
    {
        T strongReference;
        WeakReference weakReference;
        public bool isWeak;

        public ContentReference(T data)
        {
            this.strongReference = data;
            this.weakReference = null;
            this.isWeak = false;
        }

        public T GetContent()
        {
            if (strongReference != null)
                return strongReference;

            if (weakReference != null)
            {
                //if the weak reference is not yet garbage collected
                if (weakReference.IsAlive)
                {
                    //promote it back to strong reference as it just got accessed
                    Strengthen();
                    return strongReference;
                }
                else
                {
                    //if the weak reference has got collected, set it to null so that we ont check again
                    weakReference = null;
                }
            }

            return null;
        }

        //weakens the reference so as to make the contents available forgarbage collection
        public void Weaken()
        {
            weakReference = new WeakReference(strongReference);
            strongReference = null;
            isWeak = true;
        }

        public void Strengthen()
        {
            strongReference = (T)weakReference.Target;
            weakReference = null;
            isWeak = false;
        }

    }

    /*
     * Class LRUCacheNode:
     * abstraction used to store data against key in cache
     * 
     */
    class LRUCacheNode<K,V> : IComparable where V:class
    {
        //back-reference to key which points to this node to facilitate clean-up of stale nodes from garbage collection
        public K key;

        //reference to content
        public ContentReference<V> contents;

        //used to implement LRU eviction policy
        public DateTime LastAccessTime;


        public LRUCacheNode(K key, V contents)
        {
            this.key = key;
            this.contents = new ContentReference<V>(contents);
            this.LastAccessTime = DateTime.Now;
        }

        /*
         * compares recency of access of current node with that of a given node
         * returns:
         * 0 : if both nodes had same access time
         * 1 : if current node has later access time than given node (current node was accessed more recently and so the other node is a better candidate for garbage collection)
         * -1 : if current node has earlier access time than given node (other node was accessed more recently and so the current node is a better candidate for garbage collection)
         */
        public int CompareTo(object obj)
        {
            LRUCacheNode<K,V> other = obj as LRUCacheNode<K,V>;

            if (other != null)
            {
                return this.LastAccessTime.CompareTo(other.LastAccessTime);
            }
            else
                throw new Exception("object incompatible to LRUCacheNode");
        }
    }




    public interface Cache<K,V>
    {

        V Get(K Key);
        void Put(K Key, V Value);
        void Remove(K Key);
        bool ContainsKey(K Key);
    }


    public class LRUCache<K,V> : Cache<K,V> where V : class
    {
        //maintains mapping from key (filename) to value (contents of file)
        Dictionary<K, LRUCacheNode<K,V>> lookup = null;

        //minimum population to be present in cache before oldest file is marked for eviction
        private int numElementsInCacheBeforeEvictionStarts;

        //element identified for eviction by previous run of house-keeping thread
        LRUCacheNode<K,V> cleanupCandidate = null;

        //specifies how often the cleanup routine should be invoked
        private int cleanupFrequencyInms;


        public LRUCache(int cleanupFrequencyInms = 10000, int numElementsInCacheBeforeEvictionStarts = 1000)
        {
            lookup = new Dictionary<K, LRUCacheNode<K,V>>();

            this.cleanupFrequencyInms = cleanupFrequencyInms;
            this.numElementsInCacheBeforeEvictionStarts = numElementsInCacheBeforeEvictionStarts;

            StartHouseKeepingThread();
        }

        private void StartHouseKeepingThread()
        {
            Thread housekeepingThread = new Thread(new ThreadStart(KeepCleaningUp))
            {
                IsBackground = true, //setting as daemon so it does not block application exit
                Name = "HouseKeepingThread"
            };

            housekeepingThread.Start();
        }

        private void KeepCleaningUp()
        {
            while (true)
            {
                Cleanup();
                Thread.Sleep(cleanupFrequencyInms);
            }
        }

        private void Cleanup()
        {
            //if the previous run of cleanup routine identified an element for eviction
            if (cleanupCandidate != null)
            {
                //if the identified element got evicted through garbage collection
                if (cleanupCandidate.contents.GetContent() == null)
                {
                    //clean-up the entry in hashtable to reflect that value for the key is no longer available in cache
                    lookup.Remove(cleanupCandidate.key);
                    cleanupCandidate = null;
                }

                //if the cleanup candidate got accessed in the meanwhile, it is not longer a candidate for eviction
                if (!cleanupCandidate.contents.isWeak)
                    cleanupCandidate = null;
            }

            //if the cache has enough elements as to warrant eviction (as specified during initialization)
            if (lookup.Count > numElementsInCacheBeforeEvictionStarts)
            {
                //if the element identified earlier for eviction(if any) got evicted by garbage collection
                // or the candidate identified for eviction for accessed thereby making a case for its retention,
                //then proceed with finding a new candidate. if the reference is still alive, it implies that 
                //the system has enough memory left to proceed without eviction and there is no need to mark 
                //more elements for eviction.C Note : This implementation would maintain at most one element (the one with 
                //least recent access time) for eviction at any given point of time
                if ((cleanupCandidate == null) || (!cleanupCandidate.contents.isWeak))
                {
                    foreach (LRUCacheNode<K,V> node in lookup.Values)
                    {
                        //initialize candidate with first node encountered during traversal
                        if (cleanupCandidate == null)
                            cleanupCandidate = node;
                        else
                        {   //if the last time the current node got acccessed is earlier than current candidate's access time
                            if (node.CompareTo(cleanupCandidate) < 0)
                            {
                                //then the current node becomes the new candidate
                                cleanupCandidate = node;
                            }
                        }
                    }
                }

                if (cleanupCandidate != null)
                {
                    //make the identified clean-up candidate available for garbage collection
                    cleanupCandidate.contents.Weaken();
                }
            }
        }

        public bool ContainsKey(K Key)
        {
            bool result = false;

            //if the key is available in cache
            if (lookup.ContainsKey(Key))
            {
                //and if it is not a stale entry (ie. not an entry marked for eviction and got garbage collected)
                //note : this is effectively "touch"-ing the entry and will update the last access-time
                //rationale : checking for existence of key indicates the client's interest in this element and warrants retention
                LRUCacheNode<K,V> contents = lookup[Key]; 
                if ((contents != null) && (contents.contents.GetContent() != null))
                {
                    contents.LastAccessTime = DateTime.Now;
                    //we have a valid entry for the key in cache
                    result = true;
                }
                else
                {
                    //clean up the cache if it turned out to be a stale entry
                    Remove(Key);
                }
            }
            return result;
        }

        //get the contents for the given key
        public V Get(K Key)
        {
            if (lookup.ContainsKey(Key))
            {
                LRUCacheNode<K,V> node = lookup[Key];
                node.LastAccessTime = DateTime.Now;
                V contents = node.contents.GetContent();
                if (node.contents.GetContent() != null)
                {
                    return contents;
                }
                else
                {
                    //if the contents got garbage collected, clean-up the entry from cache
                    Remove(Key);
                    throw new KeyNotFoundException("key:" + Key);
                }
            }
            else
                throw new KeyNotFoundException("key:" + Key);
        }

        /*
        //cache the contents for the given key
        public void Put(K Key, V Value)
        {
            LRUCacheNode<K, V> node = new LRUCacheNode<K, V>(Key, Value);
            lookup[Key] = node;
        }
         */

        //cache the contents for the given key
        public void Put(K Key, V Value)
        {
            Put(Key, Value, 0);
        }

        private int maxPutAttemptsBeforeFailure = 3;

        private void Put(K Key, V Value, int attempt)
        {
            try
            {
                LRUCacheNode<K, V> node = new LRUCacheNode<K, V>(Key, Value);
                lookup[Key] = node;
            }
            catch (Exception e)
            {
                if (attempt < maxPutAttemptsBeforeFailure)
                {
                    Cleanup();
                    Put(Key, Value, attempt + 1);
                }
                else
                    throw e;
            }
        }

        public void Remove(K Key)
        {
            lookup.Remove(Key);
        }

    }
}
