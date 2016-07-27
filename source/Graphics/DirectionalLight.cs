using System;
using Microsoft.Xna.Framework;
using Sol2E.Core;

namespace Sol2E.Graphics
{
    /// <summary>
    /// A Component, which mimics xna's DirectionalLight and contains fields like
    /// Direction, DiffuseColor, SpecularColor and an Enabled flag.
    /// </summary>
    [Serializable]
    public class DirectionalLight : Component
    {
        public DirectionalLight()
        {
            Direction = Vector3.One * -1f;
            DiffuseColor = Color.Wheat.ToVector3();
            SpecularColor = Color.Wheat.ToVector3();
            Enabled = true;
        }

        private Vector3 _direction;
        public Vector3 Direction
        {
            get
            {
                return _direction;
            }
            set
            {
                if (value != _direction)
                {
                    _direction = value;
                    ComponentChangedEvent<DirectionalLight>.Invoke(this, "Direction");
                }
            }
        }

        private Vector3 _diffuseColor;
        public Vector3 DiffuseColor
        {
            get
            {
                return _diffuseColor;
            }
            set
            {
                if (value != _diffuseColor)
                {
                    _diffuseColor = value;
                    ComponentChangedEvent<DirectionalLight>.Invoke(this, "DiffuseColor");
                }
            }
        }

        private Vector3 _specularColor;
        public Vector3 SpecularColor
        {
            get
            {
                return _specularColor;
            }
            set
            {
                if (value != _specularColor)
                {
                    _specularColor = value;
                    ComponentChangedEvent<DirectionalLight>.Invoke(this, "SpecularColor");
                }
            }
        }

        private bool _enabled;
        public bool Enabled
        {
            get
            {
                return _enabled;
            }
            set
            {
                if (value != _enabled)
                {
                    _enabled = value;
                    ComponentChangedEvent<DirectionalLight>.Invoke(this, "Enabled");
                }
            }
        }
    }
}
