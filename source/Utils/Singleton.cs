
namespace Sol2E.Utils
{
    /// <summary>
    /// Generic singleton implementation. Use it like this:
    /// 
    /// MySingletonClass : Singleton&lt;MySingletonClass&gt;
    /// </summary>
    /// <typeparam name="T">generic type parameter (has to be a reference type)</typeparam>
    public abstract class Singleton<T> where T : class
    {
        private static readonly T _instance;
        public static T Instance
        {
            get { return _instance; }
        }

        // static constructor ensures thread safety and lazy initialization
        static Singleton()
        {
            _instance = InstanceCreator.CreateInstanceFromPrivateContructor<T>();
        }
    }
}
