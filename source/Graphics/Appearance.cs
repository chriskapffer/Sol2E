using System;
using Microsoft.Xna.Framework;
using Sol2E.Core;

namespace Sol2E.Graphics
{
    /// <summary>
    /// A Component which contains fields like TextureName, TextureEnabled, RenderWireFrame, Visible,
    /// DiffuseColor, EmissiveColor, SpecularColor and SpecularPower.
    /// </summary>
    [Serializable]
    public class Appearance : Component
    {
        // static default instance, used if a Movement instance
        // is necessary but not existent in a given context
        public static Appearance Default = new Appearance { Visible = false };

        public Appearance()
        {
            TextureName = string.Empty;
            TextureEnabled = false;
            RenderWireframe = false;
            Visible = true;

            DiffuseColor = Color.White;
            EmissiveColor = Color.Black;
            SpecularColor = Color.White;
            SpecularPower = 16f;
        }

        private string _textureName;
        public string TextureName
        {
            get
            {
                return _textureName;
            }
            set
            {
                if (value != _textureName)
                {
                    string oldValue = _textureName;
                    _textureName = value;
                    ComponentChangedEvent<Appearance>.Invoke(this, "TextureName", oldValue);
                }
            }
        }

        private bool _texturingEnabled;
        public bool TextureEnabled
        {
            get
            {
                return _texturingEnabled;
            }
            set
            {
                if (value != _texturingEnabled)
                {
                    _texturingEnabled = value;
                    ComponentChangedEvent<Appearance>.Invoke(this, "TextureEnabled");
                }
            }
        }

        private bool _visible;
        public bool Visible
        {
            get
            {
                return _visible;
            }
            set
            {
                if (value != _visible)
                {
                    _visible = value;
                    ComponentChangedEvent<Appearance>.Invoke(this, "Visible");
                }
            }
        }

        private bool _renderWireframe;
        public bool RenderWireframe
        {
            get
            {
                return _renderWireframe;
            }
            set
            {
                if (value != _renderWireframe)
                {
                    _renderWireframe = value;
                    ComponentChangedEvent<Appearance>.Invoke(this, "RenderWireframe");
                }
            }
        }

        private Color _diffuseColor;
        public Color DiffuseColor
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
                    ComponentChangedEvent<Appearance>.Invoke(this, "DiffuseColor");
                }
            }
        }

        private Color _emissiveColor;
        public Color EmissiveColor
        {
            get
            {
                return _emissiveColor;
            }
            set
            {
                if (value != _emissiveColor)
                {
                    _emissiveColor = value;
                    ComponentChangedEvent<Appearance>.Invoke(this, "EmissiveColor");
                }
            }
        }

        private Color _specularColor;
        public Color SpecularColor
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
                    ComponentChangedEvent<Appearance>.Invoke(this, "SpecularColor");
                }
            }
        }

        private float _specularPower;
        public float SpecularPower
        {
            get
            {
                return _specularPower;
            }
            set
            {
                if (Math.Abs(value - _specularPower) > float.Epsilon)
                {
                    _specularPower = value;
                    ComponentChangedEvent<Appearance>.Invoke(this, "SpecularPower");
                }
            }
        }
    }
}
