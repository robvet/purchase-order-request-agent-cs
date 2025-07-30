using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace SingleAgent.Utlls
{
    public static class PromptLoader
    {
        ///<Architecture>
        ///   PromptLoader uses a static ConcurrentDictionary as a cache.
        ///   The first time LoadPrompt is called with a given path, it reads the file 
        ///   and stores its contents in the cache. On subsequent calls with the same path, 
        ///   it returns the cached content instead of reading the file again
        ///</Architecture>
                
        private static readonly ConcurrentDictionary<string, string> _cache = new();

        public static string LoadPrompt(string path)
        {
            return _cache.GetOrAdd(path, p => File.ReadAllText(p));
        }
    }
}

// Global prompt loader for root-level prompts
namespace SingleAgent.Prompting
{
    public static class GlobalPromptLoader
    {
        private static readonly ConcurrentDictionary<string, string> _cache = new();

        public static string LoadPrompt(string path)
        {
            return _cache.GetOrAdd(path, p => File.ReadAllText(p));
        }
    }
}
