using System;
using System.Reflection;

namespace Sol2E.Utils
{
    /// <summary>
    /// Factory class which uses reflection to invoke constructors of generic types.
    /// Used for generic singleton or multiton implementations, or other situations where
    /// the underlying class is unknown.
    /// </summary>
    public static class InstanceCreator
    {
        /// <summary>
        /// Creates an instance of given type by invoking its default constructor through reflection
        /// </summary>
        /// <typeparam name="T">generic type of class to create an instance of (has to be a reference type)</typeparam>
        /// <returns>instance of T</returns>
        public static T CreateInstanceFromDefaultContructor<T>() where T : class
        {
            Type type = typeof(T);
            // retrieve default constructor
            ConstructorInfo constructor = type.GetConstructor(
                BindingFlags.Instance | BindingFlags.Public, null, Type.EmptyTypes, new ParameterModifier[0]);

            if (constructor == null)
            {
                // no default constructor found
                throw new Exception(type.FullName + " doesn't have a default constructor.");
            }

            // invoke constructor (with no params, since it is the default constructor)
            return (T)constructor.Invoke(null);
        }

        /// <summary>
        /// Creates an instance of given type by invoking its private/protected default constructor
        /// through reflection. Used to create generic singletons.
        /// </summary>
        /// <typeparam name="T">generic type of class to create an instance of (has to be a reference type)</typeparam>
        /// <returns>instance of T</returns>
        public static T CreateInstanceFromPrivateContructor<T>() where T : class
        {
            Type type = typeof(T);
#if DEBUG
            // look for public constructors, which should not exist per definition
            // do this only in debug mode, during development
            ConstructorInfo[] publicConstructors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public);
            if (publicConstructors.Length > 0)
                throw new Exception(type.FullName + " has one or more public constructors.");
#endif
            // gets private default constructor with no parameters
            ConstructorInfo privateConstructor = type.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, Type.EmptyTypes, new ParameterModifier[0]);
            if (privateConstructor == null)
            {
                throw new Exception(type.FullName + " doesn't have a private/protected default constructor.");
            }

            // invoke constructor
            return (T)privateConstructor.Invoke(null);
        }
    }
}
