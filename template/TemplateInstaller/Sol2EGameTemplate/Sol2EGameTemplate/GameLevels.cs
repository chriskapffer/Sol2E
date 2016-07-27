using System;
using Microsoft.Xna.Framework;
using Sol2E;
using Sol2E.Audio;
using Sol2E.Common;
using Sol2E.Core;
using Sol2E.Graphics;
using Sol2E.Graphics.UI;
using Sol2E.Physics;
using Sol2E.Utils;

using Environment = Sol2E.Physics.Environment;

namespace $safeprojectname$
{
    public struct ScoreKeeper
    {
        public int Current;
        public int Target;

        public ScoreKeeper(int target)
        {
            Current = 0;
            Target = target;
        }
    }

    public static class GameLevels
    {
        public const int TargetScore = 10;
        public static ScoreKeeper ScoreLevelOne = new ScoreKeeper(TargetScore);
        public static int VictoryEntityId = IDPool.InvalidID;

        /// <summary>
        /// This creates a sample scene with a nice little game in it.
        /// </summary>
        /// <param name="level">level to be set up</param>
        public static void SetupLevelOne(Scene level)
        {
            // create an entity, to play a victory sound.
            Entity victoryEntity = Entity.Create();
            victoryEntity.AddComponent(new AudioSource("sounds/youwon"));
            victoryEntity.AssignToScene(level);
            VictoryEntityId = victoryEntity.Id;

            // set up level settings
            Entity sceneSettings = Entity.Create();
            sceneSettings.AddComponent(new Ambience
                { FogEnabled = true, ClearColor = Color.LightGreen, FogColor = Color.LightGreen, FogEnd = 8f });
            sceneSettings.AddComponent(new Environment { Gravity = Vector3.Down * 9.81f });
            sceneSettings.AddComponent(new AudioSource("sounds/music") { PlaysOnStartUp = true, Volume = 0.25f });
            sceneSettings.AssignToScene(level);

            SetupDefaultLighting(level);

            // create something to stand on
            for (int i = -10; i <= 10; i++)
            {
                for (int j = -10; j <= 10; j++)
                {
                    // this is a tiled ground to better account for fog effect
                    var groundTile = GameEntities.CreateCube(string.Empty, false, Color.Gray, Color.Gray,
                        new Vector3(2f, 1f, 2f), new Vector3(2f * i, -2f, 2f * j), Matrix.Identity);
                    // to identifiy the ground from other objects on collision
                    groundTile.AddComponent(new EntityInfo("ground"));
                    groundTile.AssignToScene(level);
                }
            }

            // put another platform underneath the first one, and add a collision script to it, so that the
            // player gets reset on touch.
            var resetPane = GameEntities.CreateCube(string.Empty, false, Color.Gray, Color.Gray,
                new Vector3(60f, 1f, 60f), new Vector3(0, -4f, 0), Matrix.Identity);
            resetPane.AddComponent(new ScriptCollection<CollisionScript>(new PlayerResetScript()));
            resetPane.Get<Appearance>().Visible = false;
            resetPane.AssignToScene(level);

            // create some space ships, which are asleep, until they are shot at.
            int numOfShips = TargetScore;
            float radius = 17;
            Random rand = new Random();
            for (int i = 0; i < numOfShips; i++)
            {
                float randomness = (float) rand.NextDouble();
                float theta = i * MathHelper.TwoPi / numOfShips;
                float x = (float)Math.Cos(theta) * (randomness * radius + 3);
                float z = (float)Math.Sin(theta) * (randomness * radius + 3);
                var ship = GameEntities.CreateModel("models/Ship", Matrix.Identity, true, 0.001f,
                    new Vector3(x, 0.5f, z), Matrix.CreateRotationY(MathHelper.TwoPi * (float)rand.NextDouble()), Color.Wheat, Color.Wheat, 10, true);
                
                ship.Get<Appearance>().TextureEnabled = false;
                ship.Get<Collider>().IsAffectedByGravity = false;
                ship.AddComponent(new EntityInfo("ship"));
                ship.AddComponent(new AudioSource("sounds/smash") { Is3D = true } );
                ship.AddComponent(new ScriptCollection<CollisionScript>(new ShipCollisionScript()));
                ship.AssignToScene(level);
            }
        }

        /// <summary>
        /// Creates three directional lights and adds them to the given scene.
        /// </summary>
        /// <param name="scene">scene to add the lights to</param>
        private static void SetupDefaultLighting(Scene scene,
            bool lightOneEnabled = true, bool lightTwoEnabled = true, bool lightThreeEnabled = true)
        {
            var lightOne = new DirectionalLight
            {
                Direction = new Vector3(-0.5265408f, -0.5735765f, -0.6275069f),
                DiffuseColor = new Vector3(1f, 0.9607844f, 0.8078432f),
                SpecularColor = new Vector3(1f, 0.9607844f, 0.8078432f),
                Enabled = lightOneEnabled
            };

            var lightTwo = new DirectionalLight
            {
                Direction = new Vector3(0.7198464f, 0.3420201f, 0.6040227f),
                DiffuseColor = new Vector3(0.9647059f, 0.7607844f, 0.4078432f),
                SpecularColor = Vector3.Zero,
                Enabled = lightTwoEnabled
            };

            var lightThree = new DirectionalLight
            {
                Direction = new Vector3(0.4545195f, -0.7660444f, 0.4545195f),
                DiffuseColor = new Vector3(0.3231373f, 0.3607844f, 0.3937255f),
                SpecularColor = new Vector3(0.3231373f, 0.3607844f, 0.3937255f),
                Enabled = lightThreeEnabled
            };

            Entity lightEntityOne = Entity.Create();
            lightEntityOne.AddComponent(lightOne);
            lightEntityOne.AssignToScene(scene);

            Entity lightEntityTwo = Entity.Create();
            lightEntityTwo.AddComponent(lightTwo);
            lightEntityTwo.AssignToScene(scene);

            Entity lightEntityThree = Entity.Create();
            lightEntityThree.AddComponent(lightThree);
            lightEntityThree.AssignToScene(scene);
        }
    }
}
