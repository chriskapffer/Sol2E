using System;
using Sol2E.Core;

namespace Sol2E.Physics
{
    /// <summary>
    /// A Component, which contains Bounciness, KineticFriction and StaticFriction.
    /// </summary>
    [Serializable]
    public class Surface : Component
    {
        public Surface()
        {
            Bounciness = 0.0f;
            KineticFriction = 0.6f;
            StaticFriction = 0.8f;
        }

        private float _bounciness;
        public float Bounciness
        {
            get
            {
                return _bounciness;
            }
            set
            {
                if (Math.Abs(value - _bounciness) > float.Epsilon)
                {
                    _bounciness = value;
                    ComponentChangedEvent<Surface>.Invoke(this, "Bounciness");
                }
            }
        }

        private float _kineticFriction;
        public float KineticFriction
        {
            get
            {
                return _kineticFriction;
            }
            set
            {
                if (Math.Abs(value - _kineticFriction) > float.Epsilon)
                {
                    _kineticFriction = value;
                    ComponentChangedEvent<Surface>.Invoke(this, "KineticFriction");
                }
            }
        }

        private float _staticFriction;
        public float StaticFriction
        {
            get
            {
                return _staticFriction;
            }
            set
            {
                if (Math.Abs(value - _staticFriction) > float.Epsilon)
                {
                    _staticFriction = value;
                    ComponentChangedEvent<Surface>.Invoke(this, "StaticFriction");
                }
            }
        }

        public float Friction
        {
            set
            {
                KineticFriction = value;
                StaticFriction = value;
            }
        }
    }
}
