using System;
using Microsoft.Xna.Framework;
using Sol2E.Core;

namespace Sol2E.Graphics
{
    /// <summary>
    /// A Component which contains fields like AmbientLightColor, ClearColor, FogColor,
    /// FogEnabled, FogStart and FogEnd.
    /// Shoud only exist once per Scene
    /// </summary>
    [Serializable]
    public class Ambience : Component
    {
        public Ambience()
        {
            AmbientLightColor = new Color(0.05333332f, 0.09882354f, 0.1819608f);
            ClearColor = Color.CornflowerBlue;
            FogColor = Color.CornflowerBlue;

            FogEnabled = false;
            FogStart = 2.0f;
            FogEnd = 10.0f;
        }

        private Color _ambientLightColor;
        public Color AmbientLightColor
        {
            get
            {
                return _ambientLightColor;
            }
            set
            {
                if (value != _ambientLightColor)
                {
                    _ambientLightColor = value;
                    ComponentChangedEvent<Ambience>.Invoke(this, "AmbientLightColor");
                }
            }
        }

        private Color _clearColor;
        public Color ClearColor
        {
            get
            {
                return _clearColor;
            }
            set
            {
                if (value != _clearColor)
                {
                    _clearColor = value;
                    ComponentChangedEvent<Ambience>.Invoke(this, "ClearColor");
                }
            }
        }

        private Color _fogColor;
        public Color FogColor
        {
            get
            {
                return _fogColor;
            }
            set
            {
                if (value != _fogColor)
                {
                    _fogColor = value;
                    ComponentChangedEvent<Ambience>.Invoke(this, "FogColor");
                }
            }
        }

        private bool _fogEnabled;
        public bool FogEnabled
        {
            get
            {
                return _fogEnabled;
            }
            set
            {
                if (value != _fogEnabled)
                {
                    _fogEnabled = value;
                    ComponentChangedEvent<Ambience>.Invoke(this, "FogEnabled");
                }
            }
        }

        private float _fogStart;
        public float FogStart
        {
            get
            {
                return _fogStart;
            }
            set
            {
                if (Math.Abs(value - _fogStart) > float.Epsilon)
                {
                    _fogStart = value;
                    ComponentChangedEvent<Ambience>.Invoke(this, "FogStart");
                }
            }
        }

        private float _fogEnd;
        public float FogEnd
        {
            get
            {
                return _fogEnd;
            }
            set
            {
                if (Math.Abs(value - _fogEnd) > float.Epsilon)
                {
                    _fogEnd = value;
                    ComponentChangedEvent<Ambience>.Invoke(this, "FogEnd");
                }
            }
        }
    }
}
