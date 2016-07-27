using System;
using System.Collections.Generic;

namespace Sol2E.Core
{
    /// <summary>
    /// Custom attribute to decorate properties with a name
    /// </summary>
    public class ComponentPropertyAttribute : Attribute
    {
        public string Name { get; set; }
        public Component Host { get; set; } // this won't work. See explanation below.
    }

    /// <summary>
    /// Generic component property. This was an attempt to get rid of redundant code.
    /// Instead of writing something like this all the time
    /// 
    /// private Vector3 _linearVelocity;
    /// public Vector3 LinearVelocity
    /// {
    ///     get
    ///     {
    ///         return _linearVelocity;
    ///     }
    ///     set
    ///     {
    ///         if (value != _linearVelocity)
    ///         {
    ///             _linearVelocity = value;
    ///             ComponentChangedEvent&lt;Movement&gt;.Invoke(this, "LinearVelocity");
    ///         }
    ///     }
    /// }
    /// 
    /// the ComponentProperty class would have allowed me to write this instead
    /// 
    /// [ComponentPropertyAttribute(Name = "LinearVelocity")]
    /// ComponentProperty&lt;Vector3, Movement&gt; LinearVelocity { get; set; }
    /// 
    /// Unfortunately this is not possible, because ComponentChangedEvent&lt;Movement&gt;.Invoke(..)
    /// needs a reference to the instance of Movement where LinearVelocity resides in.
    /// 
    /// I even tried putting a reference inside ComponentPropertyAttribute like this
    /// 
    /// [ComponentPropertyAttribute(Name = "LinearVelocity", Host = this)]
    /// ComponentProperty&lt;Vector3, Movement&gt; LinearVelocity { get; set; }
    /// 
    /// But that fails to compile at 'Host = this', because an attribute argument must be
    /// a constant expression. So this is why the whole thing is useless.
    /// </summary>
    /// <typeparam name="TValue">type parameter of wrapped type (doesn't have to be a reference type,
    /// because then it would not be able to use float, int, or Vector3)</typeparam>
    /// <typeparam name="TOwner">type parameter of property owner (has to be a type of Component)</typeparam>
    [Serializable]
    public class ComponentProperty<TValue, TOwner> where TOwner : Component
    {
        private TValue _value;
        public TValue Value
        {
            get
            {
                return _value;
            }
            set
            {
                // use generic equality operator, because TValue can't be restricted to reference types
                if (!EqualityComparer<TValue>.Default.Equals(_value, value))
                {
                    _value = value;

                    // use reflection to get the attribute the property was decorated with
                    var attribute = (ComponentPropertyAttribute)Attribute.GetCustomAttribute(
                        typeof(ComponentProperty<TValue, TOwner>),
                        typeof(ComponentPropertyAttribute));

                    ComponentChangedEvent<TOwner>.Invoke(
                        attribute.Host as TOwner, // Host wasn't assignable in the first place. So, not possible!
                        attribute.Name);
                }
            }
        }

        // implicit casting operator, neccessary to use the class as mentioned above
        public static implicit operator TValue(ComponentProperty<TValue, TOwner> value)
        {
            return value.Value;
        }

        // implicit casting operator, neccessary to use the class as mentioned above
        public static implicit operator ComponentProperty<TValue, TOwner>(TValue value)
        {
            return new ComponentProperty<TValue, TOwner> { Value = value };
        }
    }
}
