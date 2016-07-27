using System;
using Sol2E.Core;

namespace Sol2E.Audio
{
    /// <summary>
    /// A Component, which marks a listener entity but doesn't contain any fields.
    /// </summary>
    [Serializable]
    public class AudioListener : Component
    {
        // static instance for faster look up
        public static AudioListener ActiveListener {get; private set;}

        // makes this listener instance active or not
        public bool IsActive
        {
            get
            {
                return this == ActiveListener;
            }
            set
            {
                if (value && this != ActiveListener)
                    ActiveListener = this;
                else if (!value && this == ActiveListener)
                    ActiveListener = null;
            }
        }

        public AudioListener()
        {
            if (ActiveListener == null)
                ActiveListener = this;
        }
    }
}
