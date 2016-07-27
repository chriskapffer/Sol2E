using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Sol2E;
using Sol2E.Core;
using Sol2E.Common;
using Sol2E.Input;

namespace GameTemplate
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class GameTemplate1 : Sol2EGame
    {
        public GameTemplate1()
            : base("Title of your Game", false) // change this to 'true' to run your game in fullscreen mode
        {
            Content.RootDirectory = "Content";

            // if you don't call CreateASampleGame() in your Initialize() method,
            // then you might want to uncomment these lines, to be able to exit the game.

            //Entity scriptingEntity = Entity.Create();
            //scriptingEntity.AddComponent(new ScriptCollection<InputScript>(new ExitOnKeyPress(InputSource.KeyEscape)));
            //Scene.Global.AddEntity(scriptingEntity);

            // this enables you to access the games debug information through a user interface
            GameMenu.CreateDebugInfo(this, false); // second parameter indicates, if it is initially visible
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here and call base.Initialize() afterwards

            CreateASampleGame(); // remove this line to start from scratch

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // TODO: use this.Content to load your game content here

            StartTheSampleGame(); // remove this line to start from scratch
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// This gives you a starting point on how to create games with the Sol2E engine.
        /// </summary>
        private void CreateASampleGame()
        {
            // create a player instance
            Entity playerEntity = GameEntities.CreatePlayer();
            GameEntities.BecomeFirstPersonController(playerEntity);
            Scene.Global.AddEntity(playerEntity);

            // create a level
            Scene levelOne = SceneManager.NewScene("MyFirstLevel", CreateResourceManager());
            GameLevels.SetupLevelOne(levelOne);

            // set up a game menu
            GameMenu.Initialize();
        }

        /// <summary>
        /// This starts the sample game by loading one of the scene, which have been set up
        /// previously by CreateASampleGame().
        /// </summary>
        private void StartTheSampleGame()
        {
            SceneManager.LoadScene("MyFirstLevel");
        }
    }
}
