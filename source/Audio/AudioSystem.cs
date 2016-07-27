using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;
using Sol2E.Common;
using Sol2E.Core;

using XnaAudioEmitter = Microsoft.Xna.Framework.Audio.AudioEmitter;
using XnaAudioListener = Microsoft.Xna.Framework.Audio.AudioListener;

namespace Sol2E.Audio
{
    /// <summary>
    /// The audio system.
    /// For general explanatino on what a system does, see the documentation of IDomainSystem.
    /// Components used by this system are Transform, Movement, and AudioSource and AudioListener.
    /// 
    /// The system updates the positions of AudioListener and AudioSources and set the sources
    /// play state.
    /// </summary>
    public class AudioSystem : AbstractDomainSystem
    {
        #region Properties and Fields

        protected readonly XnaAudioListener Listener;
        protected readonly XnaAudioEmitter Emitter;

        // collection of xna sound effect instances, each associated with an entity id
        protected readonly IDictionary<int, SoundEffectInstance> SoundInstances;
        // collection of xna sound effects, each associated with the name of the corresponding sound
        protected readonly IDictionary<string, SoundEffect> SoundEffects;

        public float MasterVolume
        {
            get { return SoundEffect.MasterVolume; }
            set { SoundEffect.MasterVolume = value; }
        }

        public float DistanceScale
        {
            get { return SoundEffect.DistanceScale; }
            set { SoundEffect.DistanceScale = value; }
        }

        public float DopplerScale
        {
            get { return SoundEffect.DopplerScale; }
            set { SoundEffect.DopplerScale = value; }
        }

        public float SpeedOfSound
        {
            get { return SoundEffect.SpeedOfSound; }
            set { SoundEffect.SpeedOfSound = value; }
        }

        #endregion

        public AudioSystem()
            : base("Audio")
        {
            Listener = new XnaAudioListener();
            Emitter = new XnaAudioEmitter();

            SoundInstances = new ConcurrentDictionary<int, SoundEffectInstance>();
            SoundEffects = new ConcurrentDictionary<string, SoundEffect>();

            DistanceScale = 5;
            DopplerScale = 0.1f;
        }

        #region Implementation of AbstractDomainSystem

        /// <summary>
        /// Initializes internal resources, which might be not available at creation.
        /// </summary>
        public override void Initialize()
        {
            // register to changed events of this component, to handle them appropriately
            ComponentChangedEvent<AudioSource>.ComponentChanged += AudioSourceChanged;
        }

        /// <summary>
        /// Updates audio resources.
        /// </summary>
        /// <param name="deltaTime">elapsed game time in total seconds</param>
        protected override void Update(float deltaTime)
        {
            // update listener
            Transform listenerTransform = null;
            Movement listenerMovement = null;
            if (AudioListener.ActiveListener != null)
            {
                listenerTransform = AudioListener.ActiveListener.GetHostingEntity().Get<Transform>();
                listenerMovement = AudioListener.ActiveListener.GetHostingEntity().Get<Movement>();
            }

            // set listener position from the hosting entity's transform component
            // or set it to default (0, 0, 0) if there is no entity containing a component of type AudioListener
            if (listenerTransform == null) listenerTransform = Transform.Default;
            Listener.Position = listenerTransform.Position;
            Listener.Forward = listenerTransform.Forward;
            Listener.Up = listenerTransform.Up;

            if (listenerMovement != null) 
                Listener.Velocity = listenerMovement.LinearVelocity;

            // update sources
            foreach (var pair in SoundInstances)
            {
                Entity entity = Entity.GetInstance(pair.Key);
                SoundEffectInstance sound = pair.Value;

                var audioSource = entity.Get<AudioSource>();
                if (audioSource != null && !sound.IsDisposed)
                {
                    // update properties of audio source from sound state
                    audioSource.IsPlaying = sound.State == SoundState.Playing;
                    audioSource.IsPaused = sound.State == SoundState.Paused;
                    audioSource.IsStopped = sound.State == SoundState.Stopped;

                    // if sound is playing and if it is 3d, update it's position from
                    // the hosting entity's transform component
                    if (audioSource.IsPlaying && audioSource.Is3D)
                    {
                        var transform = entity.Get<Transform>() ?? Transform.Default;

                        Emitter.Position = transform.Position;
                        Emitter.Forward = transform.Forward;
                        Emitter.Up = transform.Up;

                        sound.Apply3D(Listener, Emitter);
                    }
                }
            }
        }

        /// <summary>
        /// Cleans up internal resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources;
        /// false to release only unmanaged resources</param>
        protected override void Dispose(bool disposing)
        {
            // unregister from changed events of this component
            ComponentChangedEvent<AudioSource>.ComponentChanged -= AudioSourceChanged;
        }

        #endregion

        #region Event Handling for Scene and Entity Changes

        /// <summary>
        /// This method should register to SceneChangedEvents and handle them appropriately.
        /// </summary>
        /// <param name="sender">affected scene</param>
        /// <param name="eventType">whether an entity was added or removed</param>
        /// <param name="entity">affected entity</param>
        public override void SceneChanged(Scene sender, SceneEventType eventType, Entity entity)
        {
            if (eventType == SceneEventType.EntityAdded)
            {
                AddEntityResources(entity);
            }
            else
            {
                RemoveEntityResources(entity);
            }
        }

        /// <summary>
        /// This method should register to EntityChangedEvents and handle them appropriately.
        /// </summary>
        /// <param name="sender">affected entity</param>
        /// <param name="eventType">whether a component was added, removed or deserialized</param>
        /// <param name="component">affected component</param>
        public override void EntityChanged(Entity sender, EntityEventType eventType, Component component)
        {
            switch (eventType)
            {
                case EntityEventType.ComponentAdded:
                    throw new NotImplementedException();
                case EntityEventType.ComponentRemoved:
                    throw new NotImplementedException();
                case EntityEventType.ComponentDeserialized:
                    // if the system uses this entity, determine the type
                    // of the component and tell that all its properties have been updated
                    if (SoundInstances.ContainsKey(sender.Id))
                    {
                        if (component is AudioSource)
                            AudioSourceChanged(component as AudioSource, "All", null);
                    }
                    break;
            }
        }

        #endregion

        #region Event Handling for Component Changes

        /// <summary>
        /// Handles changes of a AudioSource component.
        /// </summary>
        /// <param name="sender">component of type AudioSource</param>
        /// <param name="propertyName">name of property that changed</param>
        /// <param name="oldValue">old value of that property or null, if not specified</param>
        protected virtual void AudioSourceChanged(AudioSource sender, string propertyName, object oldValue)
        {
            // do nothing if associated entity is not used by the system
            SoundEffectInstance sound;
            Entity entity = sender.GetHostingEntity();
            if (!SoundInstances.TryGetValue(entity.Id, out sound))
                return;

            switch (propertyName)
            {
                // create a new sound effect instance for any of those property changes, if their
                // change is too fundamental or can't be applied, once an effect instance is created
                case "All":
                case "Is3D":
                case "IsLooped":
                case "AssetName":
                    sound.Dispose();
                    sound = LoadSoundEffect(sender.AssetName).CreateInstance();
                    SetupSoundInstanceFromAudioSource(sound, sender);
                    SoundInstances[entity.Id] = sound;
                    break;
                case "IsPlaying":
                    // start playback if property is true
                    if (sender.IsPlaying) sound.Play();
                    break;
                case "IsPaused":
                    // pause playback if property is true
                    if (sender.IsPaused) sound.Pause();
                    break;
                case "IsStopped":
                    // stop playback if property is true
                    if (sender.IsStopped) sound.Stop();
                    break;
                default:
                    // change these values by default
                    sound.Volume = sender.Volume;
                    sound.Pitch = sender.Pitch;
                    // do not change panning, if sound is 3d!
                    if(!sender.Is3D)
                        sound.Pan = sender.Pan;
                    break;
            }
        }

        #endregion

        #region Helper Methods

        #region Resource Management

        /// <summary>
        /// Adds all resources associated with this entity to the system.
        /// </summary>
        /// <param name="entity">entity which was added</param>
        protected virtual void AddEntityResources(Entity entity)
        {
            var audioSource = entity.Get<AudioSource>();
            if (audioSource == null)
                return;

            // if entity contains an audio source create an effect and instance and add them to their lists
            SoundEffect effect = LoadSoundEffect(audioSource.AssetName);
            SoundEffectInstance soundInstance = effect.CreateInstance();
            SetupSoundInstanceFromAudioSource(soundInstance, audioSource);
            SoundInstances.Add(entity.Id, soundInstance);
        }

        /// <summary>
        /// Removes all resources associated with this entity from the system.
        /// </summary>
        /// <param name="entity">entity which will be removed</param>
        protected virtual void RemoveEntityResources(Entity entity)
        {
            // if entity is used by the system, remove its associated resources
            SoundEffectInstance soundInstance;
            if (SoundInstances.TryGetValue(entity.Id, out soundInstance))
            {
                soundInstance.Dispose();
                SoundInstances.Remove(entity.Id);
            }

            var audioSource = entity.Get<AudioSource>();
            if (audioSource != null)
                UnloadSoundEffect(audioSource.AssetName);
        }

        /// <summary>
        /// Returns a sound effect for this sound. 
        /// If not existent the effect will be created from audio data.
        /// </summary>
        /// <param name="assetName">name of sound to load</param>
        /// <returns>sound effect</returns>
        protected virtual SoundEffect LoadSoundEffect(string assetName)
        {
            SoundEffect effect;
            if (!SoundEffects.TryGetValue(assetName, out effect))
            {
                effect = CurrentResourceManager.Load<SoundEffect>(assetName);
                SoundEffects.Add(assetName, effect);
            }
            return effect;
        }

        /// <summary>
        /// Unloads the sound effect associated with this asset name
        /// </summary>
        /// <param name="assetName">name of associated sound</param>
        protected virtual void UnloadSoundEffect(string assetName)
        {
            if (!SoundEffects.ContainsKey(assetName))
                return;

            // do not dispose. Resource manager does that!
            //SoundEffects[assetName].Dispose();
            SoundEffects.Remove(assetName);
        }

        #endregion

        /// <summary>
        /// Sets up all properties of a sound effect instance from given audio source.
        /// </summary>
        /// <param name="soundInstance">sound effect instance to initialize</param>
        /// <param name="audioSource">audio source to get data from</param>
        protected virtual void SetupSoundInstanceFromAudioSource(SoundEffectInstance soundInstance, AudioSource audioSource)
        {
            soundInstance.Volume = audioSource.Volume;
            soundInstance.Pitch = audioSource.Pitch;
            soundInstance.IsLooped = audioSource.IsLooped;
            if (audioSource.Is3D)
                // this makes the effect instance a 3d sound
                // (can't be undone, recreate it if audio source changes to 2d)
                soundInstance.Apply3D(Listener, Emitter);
            else
                soundInstance.Pan = audioSource.Pan;
            if (audioSource.PlaysOnStartUp)
                soundInstance.Play();
        }

        #endregion
    }
}
