using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Sol2E.Core;
using Sol2E.Utils;

namespace Sol2E.Common
{
    /// <summary>
    /// A Component, which contains a list of scripts, of either ChangeScript, 
    /// CollisionScript, InputScript, or any other subtypes of ScriptCollectionItem.
    /// All ScriptCollectionItem instances have to be serializable!
    /// </summary>
    /// <typeparam name="T">generic type param. Has to be of type ScriptCollectionItem</typeparam>
    [Serializable]
    public class ScriptCollection<T> : Component, IEnumerable where T : ScriptCollectionItem
    {
        private readonly ICollection<T> _scripts;

        public int ItemCount { get { return _scripts.Count; } }

        #region Constructors

        public ScriptCollection()
        {
            _scripts = new List<T>();
        }

        public ScriptCollection(T script)
            : this()
        {
            Add(script);
        }

        public ScriptCollection(IEnumerable<T> scripts)
            : this()
        {
            foreach (T script in scripts)
                Add(script);
        }

        #endregion

        #region Collection Manipulation

        /// <summary>
        /// Creates a new list from internal script collection
        /// </summary>
        /// <returns>new list</returns>
        public IList<T> ToList()
        {
            return _scripts.ToList();
        }

        /// <summary>
        /// Adds a ScriptCollectionItem to the list.
        /// </summary>
        /// <param name="script">ScriptCollectionItem to add</param>
        public void Add(T script)
        {
            if (!script.GetType().IsSerializable)
                throw new NotSupportedException("All component scripts have to be serializable.");

            // assign id to script item and fire change event after adding
            _scripts.Add(script);
            script.IdOfHostingComponent = Id;
            ComponentChangedEvent<ScriptCollection<T>>.Invoke(this, "Added", script);
        }

        /// <summary>
        /// Adds a ScriptCollectionItem to the list.
        /// If there exists a script item of the same type, it gets removed it first,
        /// to make sure that there is only one item.
        /// </summary>
        /// <param name="script"></param>
        public void AddSingle(T script)
        {
            // remove any previous scripts of this type
            foreach (var oldScript in GetAllScripts(typeof(T)))
                Remove(oldScript);

            Add(script);
        }

        /// <summary>
        /// Determines if a script item of given type is present.
        /// </summary>
        /// <param name="type">type to look for</param>
        /// <returns>true if existent, else false</returns>
        public bool Contains(Type type)
        {
            return _scripts.Any(script => script.GetType() == type);
        }

        /// <summary>
        /// Returns the one and only script item of this type.
        /// Throws an exception, if there are more than one item of this type.
        /// </summary>
        /// <typeparam name="TSub">type param</typeparam>
        /// <returns>script item as TSub, or null if no item of this type exists.</returns>
        public TSub GetSingleScript<TSub>() where TSub : T
        {
            return GetSingleScript(typeof (TSub)) as TSub;
        }

        /// <summary>
        /// Returns the one and only script item of this type.
        /// Throws an exception, if there are more than one item of this type.
        /// </summary>
        /// <typeparam name="type">type to look for</typeparam>
        /// <returns>script item, or null if no item of this type exists.</returns>
        public T GetSingleScript(Type type)
        {
            return _scripts.SingleOrDefault(script => script.GetType() == type);
        }

        /// <summary>
        /// Returns all script items of this type.
        /// </summary>
        /// <typeparam name="TSub">type param</typeparam>
        /// <returns>all script items as TSub</returns>
        public IEnumerable<TSub> GetAllScripts<TSub>() where TSub : T
        {
            return GetAllScripts(typeof(TSub)).Select(s => s as TSub);
        }

        /// <summary>
        /// Returns all script items of this type.
        /// </summary>
        /// <typeparam name="type">type to look for</typeparam>
        /// <returns>all script items of type</returns>
        public IEnumerable<T> GetAllScripts(Type type)
        {
            return _scripts.Where(script => script.GetType() == type);
        }

        /// <summary>
        /// Removes a ScriptCollectionItem from the list
        /// </summary>
        /// <param name="script">ScriptCollectionItem to remove</param>
        /// <returns>true if item was removed</returns>
        public bool Remove(T script)
        {
            if (script != null && _scripts.Remove(script))
            {
                // remove id from script item and fire change event after removing
                script.IdOfHostingComponent = IDPool.InvalidID;
                ComponentChangedEvent<ScriptCollection<T>>.Invoke(this, "Removed", script);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Removes and returns (pops) a ScriptCollectionItem of this type.
        /// Throws an exception if there are more than one item of this type in collection.
        /// </summary>
        /// <param name="type">type of ScriptCollectionItem to remove</param>
        /// <returns>item to remove, null if not existent</returns>
        public T RemoveSingle(Type type)
        {
            T script = GetSingleScript(type);
            Remove(script);
            return script;
        }

        #endregion

        #region IEnumerator Implementation

        // need this to be able to use ForEach on
        // ScriptCollection and to use collection initializer
        public IEnumerator<T> GetEnumerator()
        {
            return _scripts.GetEnumerator();
        }

        // need this to be able to use ForEach on
        // ScriptCollection and to use collection initializer
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _scripts.GetEnumerator();
        }

        #endregion
    }
}
