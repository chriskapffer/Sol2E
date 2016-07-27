using System;
using Microsoft.Xna.Framework;
using Sol2E.Core;

namespace Sol2E.Common
{
    /// <summary>
    /// A Component, which contains LinearVelocity, LinearMomentum, AngularVelocity and AngularMomentum.
    /// </summary>
    [Serializable]
    public class Movement : Component
    {
        // static default instance, used if a Movement instance
        // is necessary but not existent in a given context
        public static Movement Default = new Movement();

        public Movement()
        {
            LinearVelocity = Vector3.Zero;
            LinearMomentum = Vector3.Zero;
            AngularVelocity = Vector3.Zero;
            AngularMomentum = Vector3.Zero;
        }

        private Vector3 _linearVelocity;
        public Vector3 LinearVelocity
        {
            get
            {
                return _linearVelocity;
            }
            set
            {
                if (value != _linearVelocity)
                {
                    _linearVelocity = value;
                    ComponentChangedEvent<Movement>.Invoke(this, "LinearVelocity");
                }
            }
        }

        private Vector3 _linearMomentum;
        public Vector3 LinearMomentum
        {
            get
            {
                return _linearMomentum;
            }
            set
            {
                if (value != _linearMomentum)
                {
                    _linearMomentum = value;
                    ComponentChangedEvent<Movement>.Invoke(this, "LinearMomentum");
                }
            }
        }

        private Vector3 _angularVelocity;
        public Vector3 AngularVelocity
        {
            get
            {
                return _angularVelocity;
            }
            set
            {
                if (value != _angularVelocity)
                {
                    _angularVelocity = value;
                    ComponentChangedEvent<Movement>.Invoke(this, "AngularVelocity");
                }
            }
        }

        private Vector3 _angularMomentum;
        public Vector3 AngularMomentum
        {
            get
            {
                return _angularMomentum;
            }
            set
            {
                if (value != _angularMomentum)
                {
                    _angularMomentum = value;
                    ComponentChangedEvent<Movement>.Invoke(this, "AngularMomentum");
                }
            }
        }
    }
}
