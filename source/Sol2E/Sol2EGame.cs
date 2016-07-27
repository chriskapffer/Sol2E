using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Sol2E.Audio;
using Sol2E.Core;
using Sol2E.Graphics;
using Sol2E.Input;
using Sol2E.Physics;
using Sol2E.Utils;

using Component = Sol2E.Core.Component;

// Use CTRL+M followed by CTRL+L to open and collapse methods, comments, region and classes.
namespace Sol2E
{
    public delegate void GameEventHandler(object sender, EventArgs e);

    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Sol2EGame : Game
    {
        public event GameEventHandler DebugInfoChanged;

        public int DebugInfoUpdateInterval { get; protected set; } // in milliseconds
        public string DebugInfo { get; private set; }
        public bool IsMouseLocked
        {
            get { return InputDevice.IsMouseLocked; }
            set { InputDevice.IsMouseLocked = value; }
        }
        public bool IsPaused
        {
            get { return PhysicsSystem.IsPaused; }
            set { PhysicsSystem.IsPaused = value; }
        }

        public int WindowBackBufferWidth { get; set; }
        public int WindowBackBufferHeight { get; set; }
        public int FullScreenBackBufferWidth { get; set; }
        public int FullScreenBackBufferHeight { get; set; }
        public bool IsFullScreen
        {
            get
            {
                return XnaGraphicsDeviceManager.IsFullScreen;
            }
            set
            {
                if (value != XnaGraphicsDeviceManager.IsFullScreen)
                    ChangeWindowSize(value);
            }
        }

        public GraphicsDeviceManager XnaGraphicsDeviceManager { get; private set; }
        public GraphicsDevice XnaGraphicsDevice { get { return GraphicsDevice; } }

        public SceneManager SceneManager { get; private set; }

        public InputSystem InputSystem { get; private set; }
        public AudioSystem AudioSystem { get; private set; }
        public PhysicsSystem PhysicsSystem { get; private set; }
        public GraphicsSystem GraphicsSystem { get; private set; }

        private readonly ICollection<IDomainSystem> _domainSystems;
        private int _debugInfoUpdateTotalMiliseconds;

        public Sol2EGame(string title, bool startInFullScreen)
        {
            // set up game
            DebugInfoUpdateInterval = 500;
            IsFixedTimeStep = false;

            // set up window
            FullScreenBackBufferWidth = 1280;
            FullScreenBackBufferHeight = 720;
            WindowBackBufferWidth = (int)(FullScreenBackBufferWidth * 2f / 3f);
            WindowBackBufferHeight = (int)(FullScreenBackBufferHeight * 2f / 3f);
            XnaGraphicsDeviceManager = new GraphicsDeviceManager(this)
            {
                PreferredBackBufferWidth = startInFullScreen ? FullScreenBackBufferWidth : WindowBackBufferWidth,
                PreferredBackBufferHeight = startInFullScreen ? FullScreenBackBufferHeight : WindowBackBufferHeight,
                IsFullScreen = startInFullScreen
            };
            XnaGraphicsDeviceManager.ApplyChanges();
            Window.Title = title;

            // set up input
            InputDevice.ScreenCenter = new Vector2(
                XnaGraphicsDeviceManager.PreferredBackBufferWidth / 2f,
                XnaGraphicsDeviceManager.PreferredBackBufferHeight / 2f);
            IsMouseLocked = true;

            // set up scene manager
            SceneManager = new SceneManager {RootDirectory = "Savegames"};

            // set up domain systems
            InputSystem = new InputSystem();
            AudioSystem = new AudioSystem();
            PhysicsSystem = new PhysicsSystem();
            GraphicsSystem = new GraphicsSystem();

            _domainSystems = new List<IDomainSystem>
            {
                InputSystem, AudioSystem, PhysicsSystem, GraphicsSystem
            };

            VitalGameMethods.Game = this;
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            GraphicsSystem.GraphicsDevice = GraphicsDevice;
            SceneManager.GlobalResourceManager = CreateResourceManager();

            foreach (IDomainSystem system in _domainSystems)
            {
                system.Initialize();
                SceneManager.ScenesWillSwitchEvent += system.ScenesWillSwitch;
                SceneChangedEvent.SceneChanged += system.SceneChanged;
                EntityChangedEvent.EntityChanged += system.EntityChanged;
            }

            // if there is no scene existent after initialization,
            // we provide a default scene instead and load it.
            if (Scene.Count() <= 1)
            {
                Scene defaultScene = SceneManager.NewScene(
                    "emptyDefaultSceneBecauseNoneWasProvidedAtStartUp", CreateResourceManager());
                SceneManager.LoadScene(defaultScene);
            }

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {

        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {

        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// It was made sealed, because clients should not need override it.
        /// If you want to be able to update your custom logic create a subclass of AbstractDomainSystem
        /// and register it to the list of domain systems by calling AddDomainSystem(yourSystem).
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected sealed override void Update(GameTime gameTime)
        {
            UpdateDebugInfo(gameTime);

            foreach (IDomainSystem system in _domainSystems)
                system.Update(gameTime.ElapsedGameTime);

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// It was made sealed, because clients should not need override it.
        /// If you want to be able to use custom drawing create a subclass of AbstractGraphicsSystem
        /// and register it to the list of domain systems by calling AddDomainSystem(yourSystem).
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected sealed override void Draw(GameTime gameTime)
        {
            GraphicsSystem.Draw(gameTime.ElapsedGameTime);

            base.Draw(gameTime);
        }

        /// <summary>
        /// Releases all resources used by the Game class.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources;
        /// false to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (IDomainSystem system in _domainSystems)
                {
                    SceneManager.ScenesWillSwitchEvent -= system.ScenesWillSwitch;
                    SceneChangedEvent.SceneChanged -= system.SceneChanged;
                    EntityChangedEvent.EntityChanged -= system.EntityChanged;

                    system.Dispose();
                }

                SceneManager.Dispose();

                Cache.Clear();
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Changes the game window from windowed mode to fullscreen or vice versa.
        /// </summary>
        /// <param name="toFullScreen">true if switching to fullscreen,
        /// false if switching to windowed mode</param>
        private void ChangeWindowSize(bool toFullScreen)
        {
            int width = toFullScreen ? FullScreenBackBufferWidth : WindowBackBufferWidth;
            int height = toFullScreen ? FullScreenBackBufferHeight : WindowBackBufferHeight;

            GraphicsSystem.ResizeUserInterfaces(
                width / (float)XnaGraphicsDeviceManager.PreferredBackBufferWidth,
                height / (float)XnaGraphicsDeviceManager.PreferredBackBufferHeight);

            XnaGraphicsDeviceManager.PreferredBackBufferWidth = width;
            XnaGraphicsDeviceManager.PreferredBackBufferHeight = height;
            XnaGraphicsDeviceManager.IsFullScreen = toFullScreen;
            XnaGraphicsDeviceManager.ApplyChanges();
        }

        /// <summary>
        /// Updates debug information from current game state. The string 'DebugInfo'
        /// will internally only update every DebugInfoUpdateInterval.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        private void UpdateDebugInfo(GameTime gameTime)
        {
            // if not enough time has elapsed do nothing
            if (gameTime.TotalGameTime.TotalMilliseconds - _debugInfoUpdateTotalMiliseconds <= DebugInfoUpdateInterval)
                return;

            _debugInfoUpdateTotalMiliseconds = (int)gameTime.TotalGameTime.TotalMilliseconds;

            DebugInfo = string.Format("FPS:{0:f2} ", GraphicsSystem.FramesPerSecond)
                        + string.Format("Interval:{0:f2}ms\n", gameTime.ElapsedGameTime.TotalMilliseconds)
                        + string.Format("{0}\n", Profiler.PeriodicalOutput(gameTime.ElapsedGameTime.TotalMilliseconds))
                        + string.Format("Entities: {0}/{1} ", Entity.Count(Scene.Current) + Entity.Count(Scene.Global), Entity.Count())
                        + string.Format("Components: {0}/{1}\n", Component.Count(Scene.Current) + Component.Count(Scene.Global), Component.Count())
                        + string.Format("Memory:{0:0,0.000}kB", Constants.ToKiloBytes(GC.GetTotalMemory(true)));

            // fire event to notify interested clients
            if (DebugInfoChanged != null)
                DebugInfoChanged(this, EventArgs.Empty);
        }

        /// <summary>
        /// Adds a class which implements the IDomainSystem to the list of domain systems.
        /// Use this to register your system to the game update loop.
        /// </summary>
        /// <param name="system">custom domain system</param>
        public void AddDomainSystem(IDomainSystem system)
        {
            _domainSystems.Add(system);
        }

        /// <summary>
        /// Removes a class which implements the IDomainSystem from the list of domain system.
        /// Use this to remove one of the default systems, in order to replace it with your
        /// own subclass, e.g.:
        /// 
        /// game.RemoveDomainSystem(game.GraphicsSystem);
        /// game.AddDomainSystem(myCustomGraphicsSystemWhichCanRenderEvenMoreStuff);
        /// </summary>
        /// <param name="system"></param>
        public void RemoveDomainSystem(IDomainSystem system)
        {
            _domainSystems.Remove(system);
        }

        /// <summary>
        /// This creates and returns a new ResourceManager instance.
        /// A ResourceManager is a wrapper around xna's ContentManager.
        /// </summary>
        /// <returns>new ResourceManager instance</returns>
        public ResourceManager CreateResourceManager()
        {
            return new ResourceManager(new ContentManager(Services, Content.RootDirectory));
        }
    }

    /// <summary>
    /// Place to define Constants or other related stuff.
    /// </summary>
    public static class Constants
    {
        public static double ToKiloBytes(long bytes)
        {
            return bytes * 0.0009765625;
        }

        public static double ToMegaBytes(long bytes)
        {
            return bytes * 9.53674316E-7;
        }
    }
}
