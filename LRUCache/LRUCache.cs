/*
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LRUCache
{*/
    /*
     * Class ContentReference : 
     * used to maintain reference to data stored in cache. When a node becomes eviction 
     * candidate, reference its content is made weak so that it  becomes available for 
     * garbage collection. Space occupied by the contents would be reclaimed should the need arise.
     */ 
/*
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

            if (weakReference!=null)
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
*/
    /*
     * Class LRUCacheNode:
     * abstraction used to store data against key in cache
     * 
     */ 
/*
class LRUCacheNode : IComparable
    {
        //back-reference to key which points to this node to facilitate clean-up of stale nodes from garbage collection
        public string key;

        //reference to content
        public ContentReference<string> contents;

        //used to implement LRU eviction policy
        public DateTime LastAccessTime;


        public LRUCacheNode(string key, string contents)
        {
            this.key = key;
            this.contents = new ContentReference<string>(contents);
            this.LastAccessTime = DateTime.Now;
        }
*/
        /*
         * compares recency of access of current node with that of a given node
         * returns:
         * 0 : if both nodes had same access time
         * 1 : if current node has later access time than given node (current node was accessed more recently and so the other node is a better candidate for garbage collection)
         * -1 : if current node has earlier access time than given node (other node was accessed more recently and so the current node is a better candidate for garbage collection)
         */
 /*       public int CompareTo(object obj)
        {
            LRUCacheNode other = obj as LRUCacheNode;

            if (other != null)
            {
                return this.LastAccessTime.CompareTo(other.LastAccessTime);
            }
            else
                throw new Exception("object incompatible to LRUCacheNode");            
        }
    }




    public interface Cache
    {

        string Get(string Key);
        void Put(string Key, string Value);
        void Remove(string Key);
        bool ContainsKey(string Key);
    }


    public class LRUCache : Cache
    {
        //maintains mapping from key (filename) to value (contents of file)
        Dictionary<string, LRUCacheNode> lookup = null;

        //minimum population to be present in cache before oldest file is marked for eviction
        private int numElementsInCacheBeforeEvictionStarts;

        //element identified for eviction by previous run of house-keeping thread
        LRUCacheNode cleanupCandidate = null;

        //specifies how often the cleanup routine should be invoked
        private int cleanupFrequencyInms;
        

        public LRUCache(int cleanupFrequencyInms=10000, int numElementsInCacheBeforeEvictionStarts = 1000)
        {
            lookup = new Dictionary<string, LRUCacheNode>();
     
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
            if(cleanupCandidate!=null)
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
                    foreach (LRUCacheNode node in lookup.Values)
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


        public bool ContainsKey(string Key)
        {
            bool result = false;

            //if the key is available in cache
            if(lookup.ContainsKey(Key))
            {
                //and if it is not a stale entry (ie. not an entry marked for eviction and got garbage collected)
                //note : this is effectively "touch"-ing the entry and will update the last access-time
                //rationale : checking for existence of key indicates the client's interest in this element and warrants retention
                if ((lookup[Key].contents != null) && (lookup[Key].contents.GetContent() != null))
                {
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
        public string Get(string Key)
        {
            if (lookup.ContainsKey(Key))
            {
                LRUCacheNode node = lookup[Key];
                node.LastAccessTime = DateTime.Now;
                string contents = node.contents.GetContent();
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

        //cache the contents for the given key
        public void Put(string Key, string Value)
        {
            LRUCacheNode node = new LRUCacheNode(Key,Value);
            lookup[Key] = node;                          
        }

        public void Remove(string Key)
        {
            lookup.Remove(Key);
        }
        
    }
}
*/