using System;
using Sol2E.Core;

namespace Sol2E.Graphics
{
    using UI;

    /// <summary>
    /// A Component, which contains a UIElement as root, which itself may contain
    /// a deep hierachry of UIElements.
    /// </summary>
    [Serializable]
    public class UserInterface : Component
    {
        public UserInterface(string name, UIElement root)
        {
            Name = name;
            RootElement = root;
        }

        // used for identification of different user interfaces
        private string _name;
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                if (value != _name)
                {
                    _name = value;
                    ComponentChangedEvent<UserInterface>.Invoke(this, "Name");
                }
            }
        }

        private UIElement _rootElement;
        public UIElement RootElement
        {
            get
            {
                return _rootElement;
            }
            set
            {
                if (value != _rootElement)
                {
                    UIElement oldValue = _rootElement;
                    _rootElement = value;
                    ComponentChangedEvent<UserInterface>.Invoke(this, "RootElement", oldValue);
                }
            }
        }
    }
}
