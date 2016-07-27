using System;
using Microsoft.Xna.Framework;
using Sol2E.Core;

namespace Sol2E.Physics
{
    /// <summary>
    /// A Component, which contains a field called Gravity.
    /// Shoud only exist once per Scene
    /// </summary>
    [Serializable]
    public class Environment : Component
    {
        public Environment()
        {
            Gravity = Vector3.Down * 9.81f;
        }

        private Vector3 _gravity;
        public Vector3 Gravity
        {
            get
            {
                return _gravity;
            }
            set
            {
                if (value != _gravity)
                {
                    _gravity = value;
                    ComponentChangedEvent<Environment>.Invoke(this, "Gravity");
                }
            }
        }
    }
}
