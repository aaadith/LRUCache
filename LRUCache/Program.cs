using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LRUCache
{
    
    

    class FileServer
    {
        Cache<string, string> cache = null;
        
        FileServer()
        {
            cache = new LRUCache<string,string>(1000, 11000); 

        }





        static void Main(string[] args)
        {
            LRUCacheNode<string, string> a = new LRUCacheNode<string, string>("a", "a") { LastAccessTime = new DateTime(2013, 12, 1) };
            LRUCacheNode<string, string> b = new LRUCacheNode<string, string>("", "") { LastAccessTime = new DateTime(2013, 11, 1) };
            Console.WriteLine(a.CompareTo(b));
            Console.WriteLine(b.CompareTo(a));
             

        }
    }
}
