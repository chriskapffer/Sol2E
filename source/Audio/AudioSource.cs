using System;
using Sol2E.Core;

namespace Sol2E.Audio
{
    /// <summary>
    /// A Component, which contains following fields: AssetName, Is3D, IsLooped,
    /// IsPlaying, IsPaused, IsStopped, Volume, Pitch and Pan.
    /// </summary>
    [Serializable]
    public class AudioSource : Component
    {
        public AudioSource(string assetName)
        {
            AssetName = assetName;

            Is3D = false;
            IsLooped = false;
            PlaysOnStartUp = false;

            IsPlaying = false;
            IsPaused = false;
            IsStopped = true;
            
            Volume = 1f;
            Pitch = 0f;
            Pan = 0f;
        }

        private string _assetName;
        public string AssetName
        {
            get
            {
                return _assetName;
            }
            set
            {
                if (value != _assetName)
                {
                    _assetName = value;
                    ComponentChangedEvent<AudioSource>.Invoke(this, "AssetName");
                }
            }
        }

        private bool _is3D;
        public bool Is3D
        {
            get
            {
                return _is3D;
            }
            set
            {
                if (value != _is3D)
                {
                    _is3D = value;
                    ComponentChangedEvent<AudioSource>.Invoke(this, "Is3D");
                }
            }
        }

        private bool _playsOnStartUp;
        public bool PlaysOnStartUp
        {
            get
            {
                return _playsOnStartUp;
            }
            set
            {
                if (value != _playsOnStartUp)
                {
                    _playsOnStartUp = value;
                    ComponentChangedEvent<AudioSource>.Invoke(this, "PlaysOnStartUp");
                }
            }
        }

        private float _volume;
        public float Volume
        {
            get
            {
                return _volume;
            }
            set
            {
                if (Math.Abs(value - _volume) > float.Epsilon)
                {
                    _volume = value;
                    ComponentChangedEvent<AudioSource>.Invoke(this, "Volume");
                }
            }
        }

        private float _pitch;
        public float Pitch
        {
            get
            {
                return _pitch;
            }
            set
            {
                if (Math.Abs(value - _pitch) > float.Epsilon)
                {
                    _pitch = value;
                    ComponentChangedEvent<AudioSource>.Invoke(this, "Pitch");
                }
            }
        }

        private float _pan;
        public float Pan
        {
            get
            {
                return _pan;
            }
            set
            {
                if (!_is3D && Math.Abs(value - _pan) > float.Epsilon)
                {
                    _pan = value;
                    ComponentChangedEvent<AudioSource>.Invoke(this, "Pan");
                }
            }
        }

        private bool _isLooped;
        public bool IsLooped
        {
            get
            {
                return _isLooped;
            }
            set
            {
                if (value != _isLooped)
                {
                    _isLooped = value;
                    ComponentChangedEvent<AudioSource>.Invoke(this, "IsLooped");
                }
            }
        }

        private bool _isPlaying;
        public bool IsPlaying
        {
            get
            {
                return _isPlaying;
            }
            internal set
            {
                if (value != _isPlaying)
                {
                    _isPlaying = value;
                    ComponentChangedEvent<AudioSource>.Invoke(this, "IsPlaying");
                }
            }
        }

        private bool _isPaused;
        public bool IsPaused
        {
            get
            {
                return _isPaused;
            }
            internal set
            {
                if (value != _isPaused)
                {
                    _isPaused = value;
                    ComponentChangedEvent<AudioSource>.Invoke(this, "IsPaused");
                }
            }
        }

        private bool _isStopped;
        public bool IsStopped
        {
            get
            {
                return _isStopped;
            }
            internal set
            {
                if (value != _isStopped)
                {
                    _isStopped = value;
                    ComponentChangedEvent<AudioSource>.Invoke(this, "IsStopped");
                }
            }
        }

        // helper method to set playstate
        public void Play()
        {
            IsPlaying = true;
            _isPaused = false;
            _isStopped = false;
        }

        // helper method to set playstate
        public void Pause()
        {
            IsPaused = true;
            _isStopped = false;
            _isPlaying = false;
        }

        // helper method to set playstate
        public void Stop()
        {
            IsStopped = true;
            _isPlaying = false;
            _isPaused = false;
        }
    }
}
