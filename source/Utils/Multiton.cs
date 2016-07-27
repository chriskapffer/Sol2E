using System.Collections.Generic;

namespace Sol2E.Utils
{
    /// <summary>
    /// Generic multiton implementation. See: http://en.wikipedia.org/wiki/Multiton_pattern.
    /// Mimics a map of singletons. Rather than having a single instance per application
    /// this allows a single instance per key. Use it like this:
    /// 
    /// MyMultitonClass : Multiton&lt;MyMultitonClass&gt;
    /// </summary>
    /// <typeparam name="T">generic type parameter (has to be a reference type)</typeparam>
    public abstract class Multiton<T> where T : class
    {
        private static readonly Dictionary<object, T> Instances = new Dictionary<object,T>();

        /// <summary>
        /// Returns the requested multiton instance for the given key
        /// </summary>
        /// <param name="key">key to identifiy multiton</param>
        /// <returns>multiton instance</returns>
        public static T GetInstance(object key)
        {
            lock (Instances)
            {
                T instance;
                if (!Instances.TryGetValue(key, out instance))
                {
                    instance = InstanceCreator.CreateInstanceFromPrivateContructor<T>();
                    Instances.Add(key, instance);
                }
                return instance;
            }
        }

        // explicit static constructor to tell C# compiler not to mark
        // type as before field initiate (thus lazy initialization)
        static Multiton() { }
    }
}
