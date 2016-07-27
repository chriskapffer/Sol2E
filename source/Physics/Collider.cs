using System;
using Sol2E.Core;

namespace Sol2E.Physics
{
    /// <summary>
    /// A Component, which contains Mass and flags for, IsKinematic,
    /// IsPenetratable, IsDynamic and IsAffectedByGravity.
    /// </summary>
    [Serializable]
    public class Collider : Component
    {
        public Collider()
        {
            _mass = 1f;
            _isAffectedByGravity = true;
            _isPenetratable = false;
            _isKinematic = true;
            _isDynamic = false;
            _isInputControlled = false;
            _bePrecise = false;
        }

        private bool _isKinematic;
        public bool IsKinematic
        {
            get
            {
                return _isKinematic;
            }
            set
            {
                if (value != _isKinematic)
                {
                    _isKinematic = value;
                    _isDynamic = !value;
                    ComponentChangedEvent<Collider>.Invoke(this, "IsKinematic");
                }
            }
        }

        private bool _isPenetratable;
        public bool IsPenetratable
        {
            get
            {
                return _isPenetratable;
            }
            set
            {
                if (value != _isPenetratable)
                {
                    _isPenetratable = value;
                    ComponentChangedEvent<Collider>.Invoke(this, "IsPenetratable");
                }
            }
        }

        private bool _isDynamic;
        public bool IsDynamic
        {
            get
            {
                return _isDynamic;
            }
            set
            {
                if (value != _isDynamic)
                {
                    _isDynamic = value;
                    _isKinematic = !value;
                    ComponentChangedEvent<Collider>.Invoke(this, "IsDynamic");
                }
            }
        }

        private bool _isAffectedByGravity;
        public bool IsAffectedByGravity
        {
            get
            {
                return _isAffectedByGravity;
            }
            set
            {
                if (value != _isAffectedByGravity)
                {
                    _isAffectedByGravity = value;
                    ComponentChangedEvent<Collider>.Invoke(this, "IsAffectedByGravity");
                }
            }
        }

        private bool _isInputControlled;
        public bool IsInputControlled
        {
            get
            {
                return _isInputControlled;
            }
            set
            {
                if (value != _isInputControlled)
                {
                    _isInputControlled = value;
                    ComponentChangedEvent<Collider>.Invoke(this, "IsInputControlled");
                }
            }
        }

        private bool _bePrecise;
        public bool BePrecise
        {
            get
            {
                return _bePrecise;
            }
            set
            {
                if (value != _bePrecise)
                {
                    _bePrecise = value;
                    ComponentChangedEvent<Collider>.Invoke(this, "BePrecise");
                }
            }
        }

        private float _mass;
        public float Mass
        {
            get
            {
                return _mass;
            }
            set
            {
                if (Math.Abs(value - _mass) > float.Epsilon)
                {
                    _mass = value;
                    if (_isDynamic && (value <= 0 || float.IsNaN(value) || float.IsInfinity(value)))
                    {
                        _isDynamic = false;
                        _isKinematic = true;
                    }
                    ComponentChangedEvent<Collider>.Invoke(this, "Mass");
                }
            }
        }
    }
}
