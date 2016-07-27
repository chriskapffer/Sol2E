using System;
using System.Collections.Generic;
using Sol2E.Common;
using Sol2E.Core;

namespace Sol2E.Input
{
    /// <summary>
    /// Abstract script, which gets invoked if one or more input conditions are met.
    /// </summary>
    [Serializable]
    public abstract class InputScript : ScriptCollectionItem
    {
        // collection of input conditions. Only one has to be satisfied.
        public ICollection<IInputCondition> Conditions { get; set; }
        public Action<Entity, InputSource, InputState, float, object> Action { get; private set; }

        protected InputScript()
            : this(new List<IInputCondition>())
        { }

        protected InputScript(IInputCondition condition)
            : this(new List<IInputCondition>())
        {
            Conditions.Add(condition);
        }

        protected InputScript(ICollection<IInputCondition> conditions)
        {
            Conditions = conditions;
            Action = OnInput;
        }

        private bool _enabled = true;
        public bool Enabled
        {
            get
            {
                return _enabled;
            }
            set
            {
                _enabled = value;
                foreach (var inputCondition in Conditions)
                    inputCondition.Active = _enabled;
            }
        }


        public abstract void OnInput(Entity sender, InputSource source, InputState state, float deltaTime, object value);
    }
}
