using Microsoft.Xna.Framework;
using Sol2E.Core;

namespace Sol2E
{
    /// <summary>
    /// Static class to make the most important methods and properties of a Sol2EGame instance
    /// accessible from anywhere within the application. This is especially necessary for
    /// scripts which can be serialized. If they would contain references to Sol2EGame directely,
    /// they would not be serializable without making the whole game serializable as well,
    /// which is not desirable and not possible anyway.
    /// </summary>
    public static class VitalGameMethods
    {
        // Sol2EGame instance (only accessible from this assembly)
        internal static Sol2EGame Game { get; set; }

        #region Window Management

        public static Rectangle ScreenRect
        {
            get { return Game.Window.ClientBounds; }
        }
        public static bool IsFullScreen
        {
            get { return Game.IsFullScreen; }
            set { Game.IsFullScreen = value; }
        }

        #endregion

        #region Graphics Management

        public static bool RenderWireframe
        {
            get { return Game.GraphicsSystem.RenderWireframe; }
            set { Game.GraphicsSystem.RenderWireframe = value; }
        }
        public static bool TextureEnabled
        {
            get { return Game.GraphicsSystem.TextureEnabled; }
            set { Game.GraphicsSystem.TextureEnabled = value; }
        }
        public static bool LightingEnabled
        {
            get { return Game.GraphicsSystem.LightingEnabled; }
            set { Game.GraphicsSystem.LightingEnabled = value; }
        }
        public static bool PreferPerPixelLighting
        {
            get { return Game.GraphicsSystem.PreferPerPixelLighting; }
            set { Game.GraphicsSystem.PreferPerPixelLighting = value; }
        }

        #endregion

        #region Audio Management

        public static float MasterVolume
        {
            get { return Game.AudioSystem.MasterVolume; }
            set { Game.AudioSystem.MasterVolume = value; }
        }
        public static float DistanceScale
        {
            get { return Game.AudioSystem.DistanceScale; }
            set { Game.AudioSystem.DistanceScale = value; }
        }
        public static float DopplerScale
        {
            get { return Game.AudioSystem.DopplerScale; }
            set { Game.AudioSystem.DopplerScale = value; }
        }
        public static float SpeedOfSound
        {
            get { return Game.AudioSystem.SpeedOfSound; }
            set { Game.AudioSystem.SpeedOfSound = value; }
        }

        #endregion

        #region Input Management

        public static bool IsMouseVisible
        {
            get { return Game.IsMouseVisible; }
            set { Game.IsMouseVisible = value; }
        }
        public static bool IsMouseLocked
        {
            get { return Game.IsMouseLocked; }
            set { Game.IsMouseLocked = value; }
        }

        #endregion

        #region Performance Management

        public static int IterationLimit
        {
            get { return Game.PhysicsSystem.IterationLimit; }
            set { Game.PhysicsSystem.IterationLimit = value; }
        }
        public static int MaxStepsPerFrame
        {
            get { return Game.PhysicsSystem.MaxStepsPerFrame; }
            set { Game.PhysicsSystem.MaxStepsPerFrame = value; }
        }
        public static bool UseFixedTimeSteps
        {
            get { return Game.IsFixedTimeStep; }
            set { Game.IsFixedTimeStep = value; }
        }

        #endregion

        #region Playstate Management

        public static bool IsPaused
        {
            get { return Game.IsPaused; }
            set { Game.IsPaused = value; }
        }

        public static void Pause()
        {
            IsPaused = true;
        }
        public static void Resume()
        {
            IsPaused = false;
        }
        public static void Exit()
        {
            Game.Exit();
        }

        #endregion

        #region Scene Management

        public static void LoadScene(string sceneName)
        {
            Game.SceneManager.LoadScene(sceneName);
        }

        public static void LoadScene(int sceneId)
        {
            Game.SceneManager.LoadScene(sceneId);
        }

        public static void LoadNextScene()
        {
            Game.SceneManager.LoadNext();
        }

        public static void LoadPreviousScene()
        {
            Game.SceneManager.LoadPrevious();
        }

        public static void RestartCurrentScene()
        {
            Game.SceneManager.RestartCurrentScene();
        }

        public static void QuickSave()
        {
            Game.SceneManager.QuickSave();
        }
        public static void QuickRestore()
        {
            Game.SceneManager.QuickRestore();
        }

        public static void DiskSave(string sceneName)
        {
            Game.SceneManager.DiskSave(sceneName);
        }

        public static void DiskRestore(string sceneName)
        {
            Game.SceneManager.DiskRestore(sceneName);
        }

        #endregion
    }
}
