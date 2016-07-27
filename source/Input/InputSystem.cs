using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Sol2E.Core;
using Sol2E.Common;

namespace Sol2E.Input
{
    /// <summary>
    /// The input system.
    /// For general explanatino on what a system does, see the documentation of IDomainSystem.
    /// Components handled by this system are ScriptCollection&lt;InputScript&gt;.
    /// 
    /// The system updates the input device states and invokes input script actions, if their
    /// input condition is met.
    /// </summary>
    public class InputSystem : AbstractDomainSystem
    {
        #region Fields

        // collection of input scripts, each associated to its hosting entity
        protected readonly IDictionary<int, ScriptCollection<InputScript>> InputScripts;

        #endregion

        public InputSystem()
            : base("Input")
        {
            InputScripts = new ConcurrentDictionary<int, ScriptCollection<InputScript>>();
        }

        #region Implementation of AbstractDomainSystem

        /// <summary>
        /// Initializes internal resources, which might be not available at creation.
        /// </summary>
        public override void Initialize()
        {
            // register to changed events of this component, to handle them appropriately
            ComponentChangedEvent<ScriptCollection<InputScript>>.ComponentChanged += ScriptCollectionChanged;
        }

        /// <summary>
        /// Updates the input device states.
        /// </summary>
        /// <param name="deltaTime">elapsed game time in total seconds</param>
        protected override void Update(float deltaTime)
        {
            // update current input device state
            InputDevice.UpdateCurrent();

            // iterate over all script collections 
            foreach (var pair in InputScripts)
            {
                Entity entity = Entity.GetInstance(pair.Key);
                ScriptCollection<InputScript> scripts = pair.Value;
                // iterate over all scripts of a collection
                foreach (InputScript script in scripts.ToList())
                {
                    // iterate over all conditions of a script
                    foreach (IInputCondition condition in script.Conditions)
                    {
                        // do nothing if condition is disabled or not met
                        object sourceValue;
                        if (!condition.Active || !condition.IsMet(out sourceValue))
                            continue;

                        // otherwise invoke script with corresponding parameters
                        if(condition is SimpleInputCondition)
                        {
                            var simple = condition as SimpleInputCondition;
                            script.Action.Invoke(entity, simple.Source, simple.State, deltaTime, sourceValue);
                        }
                        else
                        {
                            script.Action.Invoke(entity, InputSource.Undefined, InputState.Undefined, deltaTime, sourceValue);
                        }
                    }
                }
            }

            // make current device state previous device state
            InputDevice.UpdatePrevious();
        }

        /// <summary>
        /// Cleans up internal resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources;
        /// false to release only unmanaged resources</param>
        protected override void Dispose(bool disposing)
        {
            // unregister from changed events of this component
            ComponentChangedEvent<ScriptCollection<InputScript>>.ComponentChanged -= ScriptCollectionChanged;
        }

        #endregion

        #region Event Handling for Scene and Entity Changes

        /// <summary>
        /// This method should register to SceneChangedEvents and handle them appropriately.
        /// </summary>
        /// <param name="sender">affected scene</param>
        /// <param name="eventType">whether an entity was added or removed</param>
        /// <param name="entity">affected entity</param>
        public override void SceneChanged(Scene sender, SceneEventType eventType, Entity entity)
        {
            if (eventType == SceneEventType.EntityAdded)
            {
                AddEntityResources(entity);
            }
            else
            {
                RemoveEntityResources(entity);
            }
        }

        /// <summary>
        /// This method should register to EntityChangedEvents and handle them appropriately.
        /// </summary>
        /// <param name="sender">affected entity</param>
        /// <param name="eventType">whether a component was added, removed or deserialized</param>
        /// <param name="component">affected component</param>
        public override void EntityChanged(Entity sender, EntityEventType eventType, Component component)
        {
            switch (eventType)
            {
                case EntityEventType.ComponentAdded:
                    throw new NotImplementedException();
                case EntityEventType.ComponentRemoved:
                    throw new NotImplementedException();
                case EntityEventType.ComponentDeserialized:
                    // if the system uses this entity, determine the type
                    // of the component and tell that all its properties have been updated
                    if (InputScripts.ContainsKey(sender.Id))
                    {
                        if (component is ScriptCollection<InputScript>)
                            ScriptCollectionChanged(component as ScriptCollection<InputScript>, "All", null);
                    }
                    break;
            }
        }

        #endregion

        #region Event Handling for Component Changes

        /// <summary>
        /// Handles changes of a ScriptCollection&lt;InputScript&gt; component.
        /// </summary>
        /// <param name="sender">component of type ScriptCollection&lt;InputScript&gt;</param>
        /// <param name="propertyName">name of property that changed</param>
        /// <param name="oldValue">old value of that property or null, if not specified</param>
        protected virtual void ScriptCollectionChanged(ScriptCollection<InputScript> sender, string propertyName, object oldValue)
        {
            ScriptCollection<InputScript> scripts;
            Entity entity = sender.GetHostingEntity();
            if (InputScripts.ContainsKey(entity.Id))
            {
                // if script collection changed and the hosting entity is in use, update local reference
                InputScripts[entity.Id] = sender;
            }
        }

        #endregion

        #region Resource Management

        /// <summary>
        /// Adds all resources associated with this entity to the system.
        /// </summary>
        /// <param name="entity">entity which was added</param>
        protected virtual void AddEntityResources(Entity entity)
        {
            var scripts = entity.Get<ScriptCollection<InputScript>>();
            if (scripts == null)
                return;

            // if entity contains a collection of input scripts add it to the list
            InputScripts.Add(entity.Id, scripts);
        }

        /// <summary>
        /// Removes all resources associated with this entity from the system.
        /// </summary>
        /// <param name="entity">entity which will be removed</param>
        protected virtual void RemoveEntityResources(Entity entity)
        {
            if (!InputScripts.ContainsKey(entity.Id))
                return;

            // if entity is used by the system, remove its associated script collection
            InputScripts.Remove(entity.Id);
        }

        #endregion
    }
}
