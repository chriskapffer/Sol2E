using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Sol2E.Utils;

namespace Sol2E.Core
{
    public delegate void ScenesWillSwitchEventHandler(Scene oldScene, Scene newScene, IResourceManager newResourceManager);

    /// <summary>
    /// A Class which manages Scene creation, storage and switches. It also associates each scene
    /// with a name and a resource manager.
    /// </summary>
    public class SceneManager : IDisposable
    {
        #region Properties and Fields

        public event ScenesWillSwitchEventHandler ScenesWillSwitchEvent;

        // accessor to set resource manager of Scene.Global (can't do it inside the constructor)
        public IResourceManager GlobalResourceManager
        {
            get { return _resourceManagers[Scene.Global.Id]; }
            set { _resourceManagers[Scene.Global.Id] = value; }
        }

        public string RootDirectory { get; set; }
        private readonly string _executingDirectory;

        // a dictionary, which maps scene ids to scene names
        private readonly IDictionary<string, int> _managedScenes;
        // a dictionary, which maps resource managers to scene ids
        private readonly IDictionary<int, IResourceManager> _resourceManagers;
        // a dictionary, which maps initial scene states to scene ids
        private readonly IDictionary<int, Tuple<SceneState, SceneState>> _sceneInitialStates;
        // an in memory slot for the latest snap shot of Scene.Current
        private SceneState _quickSaveCurrent;
        // an in memory slot for the latest snap shot of Scene.Global
        private SceneState _quickSaveGlobal;
        // flag to determine if Scene.Global was loaded (should happen only once)
        private bool _initialLoad;

        #endregion

        public SceneManager()
        {
            RootDirectory = string.Empty;
            _executingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            _managedScenes = new Dictionary<string, int>();
            _resourceManagers = new Dictionary<int, IResourceManager>();
            _sceneInitialStates = new Dictionary<int, Tuple<SceneState, SceneState>>();

            _quickSaveCurrent = null;
            _quickSaveGlobal = null;
            _initialLoad = true;

            Scene.Global = NewScene("defaultGlobalSceneCreatedBySceneManager", null);
        }

        public void Dispose()
        {
            if (Scene.Current != null)
            {
                // switch to null, thus invoking switch event to let
                // listeners unload the content of current scene
                OnSceneSwitch(Scene.Current, null, null);

                // unload all left overs (things that can't be disposed manually
                // (typically shared resources, such as model textures)
                IResourceManager resourceManager = _resourceManagers[Scene.Current.Id];
                if (resourceManager != null)
                    resourceManager.UnloadAll();
            }

            // dispose all resource managers
            foreach (IResourceManager resourceManager in _resourceManagers.Values)
                resourceManager.Dispose();

            _resourceManagers.Clear();
        }

        #region Scene Creation Methods

        /// <summary>
        /// Creates a new Scene and registers it and its associated resource manager within
        /// the appropriate list. 
        /// </summary>
        /// <param name="sceneName"></param>
        /// <param name="manager"></param>
        /// <returns></returns>
        public Scene NewScene(string sceneName, IResourceManager manager)
        {
            Scene newScene = null;
            if (!_managedScenes.ContainsKey(sceneName))
            {
                newScene = Scene.Create();
                _managedScenes.Add(sceneName, newScene.Id);
                _resourceManagers.Add(newScene.Id, manager);
            }
            return newScene;
        }

        #endregion

        #region Scnene Retrieval Methods

        /// <summary>
        /// Retrieves a scene instance by name
        /// </summary>
        /// <param name="sceneName">name of scene to retrieve</param>
        /// <returns>retrieved scene</returns>
        public Scene SceneByName(string sceneName)
        {
            int sceneId;
            return _managedScenes.TryGetValue(sceneName, out sceneId)
                ? Scene.GetInstance(sceneId)
                : null;
        }

        /// <summary>
        /// Gets the name of a given scene
        /// </summary>
        /// <param name="scene">scene instance</param>
        /// <returns>associated scene name</returns>
        public string NameOfScene(Scene scene)
        {
            return _managedScenes.SingleOrDefault(kv => kv.Value == scene.Id).Key;
        }

        #endregion

        #region Scene Switching Methods

        /// <summary>
        /// Loads the next scene in the queue. Ignores Scene.Global (at index 0).
        /// </summary>
        public void LoadNext()
        {
            int count = _managedScenes.Count;
            for (int i = 1; i < count; i++)
            {
                if (_managedScenes.Values.ElementAt(i) == Scene.Current.Id)
                {
                    LoadScene(_managedScenes.Values.ElementAt((i < count - 1) ? i + 1 : 1));
                    break;
                }
            }
        }

        /// <summary>
        /// Loads the previous scene in the queue. Ignores Scene.Global (at index 0).
        /// </summary>
        public void LoadPrevious()
        {
            int count = _managedScenes.Count;
            for (int i = count - 1; i > 0; i--)
            {
                if (_managedScenes.Values.ElementAt(i) == Scene.Current.Id)
                {
                    LoadScene(_managedScenes.Values.ElementAt((i > 1) ? i - 1 : count - 1));
                    break;
                }
            }
        }

        /// <summary>
        /// Loads a scene by its id
        /// </summary>
        /// <param name="sceneId">id of scene to load</param>
        public void LoadScene(int sceneId)
        {
            LoadScene(Scene.GetInstance(sceneId));
        }

        /// <summary>
        /// Loads a scene by its name
        /// </summary>
        /// <param name="sceneName">name of scene to load</param>
        public void LoadScene(string sceneName)
        {
            LoadScene(SceneByName(sceneName));
        }

        /// <summary>
        /// Loads a scene
        /// </summary>
        /// <param name="sceneToLoad">instance of scene to load</param>
        public void LoadScene(Scene sceneToLoad)
        {
            // load Scene.Global, the first time any scene is loaded. Do this only once.
            if (_initialLoad)
            {
                _initialLoad = false;
                OnSceneSwitch(null, Scene.Global, _resourceManagers[Scene.Global.Id]);
            }

            Scene oldScene = Scene.Current;

            // fire switch event, only if scene to load is not equal to current scene
            if (oldScene != sceneToLoad)
            {
                OnSceneSwitch(oldScene, sceneToLoad, _resourceManagers[sceneToLoad.Id]);

                // unload any left overs from old scene, if existent
                if (oldScene != null)
                {
                    IResourceManager resourceManager = _resourceManagers[oldScene.Id];
                    if (resourceManager != null)
                        resourceManager.UnloadAll();
                }

                // save state if scene is loaded for the first time, else restore it
                Scene.Current = sceneToLoad;
                if (!_sceneInitialStates.ContainsKey(sceneToLoad.Id))
                {
                    _sceneInitialStates.Add(sceneToLoad.Id,
                        new Tuple<SceneState, SceneState>(Scene.Global.SaveState(), sceneToLoad.SaveState()));
                }
                else
                {
                    Scene.Global.RestoreState(_sceneInitialStates[sceneToLoad.Id].Item1);
                    sceneToLoad.RestoreState(_sceneInitialStates[sceneToLoad.Id].Item2);
                }
            }
        }

        /// <summary>
        /// Restarts the current scene.
        /// </summary>
        public void RestartCurrentScene()
        {
            Scene.Global.RestoreState(_sceneInitialStates[Scene.Current.Id].Item1);
            Scene.Current.RestoreState(_sceneInitialStates[Scene.Current.Id].Item2);
        }

        /// <summary>
        /// Invokes ScenesWillSwitchEvent.
        /// </summary>
        /// <param name="oldScene">old scene</param>
        /// <param name="newScene">new scene</param>
        /// <param name="newResourceManager">resource manager of new scene</param>
        private void OnSceneSwitch(Scene oldScene, Scene newScene, IResourceManager newResourceManager)
        {
            if (ScenesWillSwitchEvent != null)
                ScenesWillSwitchEvent.Invoke(oldScene, newScene, newResourceManager);
        }

        #endregion

        #region Scene Storage Methods

        /// <summary>
        /// Takes a snap shot of Scene.Current and Scene.Global.
        /// Will be stored in memory.
        /// </summary>
        public void QuickSave()
        {
            _quickSaveCurrent = Scene.Current.SaveState();
            _quickSaveGlobal = Scene.Global.SaveState();
        }

        /// <summary>
        /// Restores Scene.Current and Scene.Global from memory.
        /// </summary>
        public void QuickRestore()
        {
            if (_quickSaveCurrent != null && _quickSaveGlobal != null)
            {
                // restore global first
                Scene.Global.RestoreState(_quickSaveGlobal);

                Scene sceneToRestore = Scene.GetInstance(_quickSaveCurrent.SceneId);
                sceneToRestore.RestoreState(_quickSaveCurrent);

                // switch scene if stored instance is not equal to Scene.Current
                LoadScene(sceneToRestore);
            }
        }

        /// <summary>
        /// Saves the state of Scene.Current to disk.
        /// </summary>
        /// <returns>true if successful</returns>
        public bool DiskSave()
        {
            return DiskSave(Scene.Current);
        }

        /// <summary>
        /// Saves the state of given scene to disk.
        /// </summary>
        /// <param name="scene">scene instance to store</param>
        /// <returns>true if successful</returns>
        public bool DiskSave(Scene scene)
        {
            return DiskSave(scene, NameOfScene(scene));
        }

        /// <summary>
        /// Saves the state of given scene to disk.
        /// </summary>
        /// <param name="name">name of scene to store</param>
        /// <returns>true if successful</returns>
        public bool DiskSave(string name)
        {
            return DiskSave(SceneByName(name), name);
        }

        /// <summary>
        /// Saves the state of given scene to disk.
        /// </summary>
        /// <param name="scene">scene instance to store</param>
        /// <param name="name">name of save file. not necessarily equal to scene name</param>
        /// <returns>true if successful</returns>
        private bool DiskSave(Scene scene, string name)
        {
            var path = Path.Combine(_executingDirectory, RootDirectory, name);

            SceneState state = scene.SaveState();
            byte[] data = Serializer.Serialize(state);
            if (Serializer.ByteArrayToFile(path + ".s2e", data))
            {
                state = Scene.Global.SaveState();
                data = Serializer.Serialize(state);
                return Serializer.ByteArrayToFile(path + ".glb.s2e", data);
            }

            return false;
        }

        /// <summary>
        /// Restores a scene from file system.
        /// </summary>
        /// <param name="name">name of save file.</param>
        /// <returns>true if successful</returns>
        public bool DiskRestore(string name)
        {
            var path = Path.Combine(_executingDirectory, RootDirectory, name);

            byte[] data;
            if (Serializer.ByteArrayFromFile(path + ".glb.s2e", out data))
            {
                // restore global first
                var state = Serializer.Deserialize<SceneState>(data);
                Scene.Global.RestoreState(state);

                if (Serializer.ByteArrayFromFile(path + ".s2e", out data))
                {
                    state = Serializer.Deserialize<SceneState>(data);
                    Scene sceneToRestore = Scene.GetInstance(state.SceneId);
                    sceneToRestore.RestoreState(state);
                    LoadScene(sceneToRestore);
                    return true;
                }
            }
            return false;
        }

        #endregion
    }
}
