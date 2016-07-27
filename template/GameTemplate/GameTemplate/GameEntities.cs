using System;
using Microsoft.Xna.Framework;
using Sol2E;
using Sol2E.Audio;
using Sol2E.Common;
using Sol2E.Core;
using Sol2E.Graphics;
using Sol2E.Input;
using Sol2E.Physics;
using Sol2E.Utils;

namespace GameTemplate
{
    public class GameEntities
    {
        // globally accessible id of player entity
        public static int PlayerId = IDPool.InvalidID;

        #region Creation Methods

        /// <summary>
        /// Creates a player instance.
        /// </summary>
        /// <returns>the player</returns>
        public static Entity CreatePlayer()
        {
            if (PlayerId != IDPool.InvalidID)
            {
                Console.WriteLine("There is already a player existent.");
                return Entity.GetInstance(PlayerId);
            }

            var inputScripts = new ScriptCollection<InputScript>
            {
                new ShootBall(20),
                new GenericKeyPressScript(InputSource.KeyF5, VitalGameMethods.QuickSave),
                new GenericKeyPressScript(InputSource.KeyF6, () =>
                    { VitalGameMethods.QuickRestore(); GameMenu.ToggleMainMenu(); }),
                new GenericKeyPressScript(InputSource.KeyF7, () =>
                    { VitalGameMethods.RestartCurrentScene(); GameMenu.ToggleMainMenu(); }),
            };

            Entity player = Entity.Create();
            player.AddComponent(new Transform { Position = new Vector3(0f, -0.25f, 0f) });
            player.AddComponent(new Camera());
            player.AddComponent(new AudioListener());
            player.AddComponent(SimpleMeshFactory.CreateCylinder(2.5f, 0.5f));
            player.AddComponent(new Movement());
            player.AddComponent(new Collider());
            player.AddComponent(new Surface());
            player.AddComponent(inputScripts);

            PlayerId = player.Id;
            return player;
        }

        /// <summary>
        /// Convenience Method to set up a cube shaped entity with a few standard components.
        /// </summary>
        /// <returns>a new entity</returns>
        public static Entity CreateCube(string texture, bool dynamic, Color diffuse, Color specular,
            Vector3 scale, Vector3 position, Matrix rotation, float mass = 1)
        {
            Quaternion orientation = Quaternion.CreateFromRotationMatrix(rotation);
            var appearance = new Appearance
            {
                TextureEnabled = texture != string.Empty,
                TextureName = texture,
                DiffuseColor = diffuse,
                SpecularColor = specular
            };

            Entity cube = Entity.Create();
            cube.AddComponent(SimpleMeshFactory.CreateCube(scale));
            cube.AddComponent(new Transform { Position = position, Orientation = orientation });
            cube.AddComponent(new Collider { IsDynamic = dynamic, Mass = mass });
            cube.AddComponent(appearance);
            return cube;
        }

        /// <summary>
        /// Convenience Method to set up an entity with a 3d model and other standard components.
        /// </summary>
        /// <returns></returns>
        public static Entity CreateModel(string assetName, Matrix localTransform, bool dynamic,
            float scale, Vector3 position, Matrix rotation, Color diffuse, Color specular, float mass = 1, bool preciseModel = false)
        {
            Quaternion orientation = Quaternion.CreateFromRotationMatrix(rotation);

            Entity model = Entity.Create();
            model.AddComponent(new ModelMesh(assetName, scale, localTransform));
            model.AddComponent(new Transform { Position = position,  Orientation = orientation });
            model.AddComponent(new Collider { BePrecise = preciseModel, IsDynamic = dynamic, Mass = mass });
            model.AddComponent(new Appearance { TextureEnabled = true, DiffuseColor = diffuse, SpecularColor = specular });
            return model;
        }

        #endregion

        #region Manipulation Methods

        /// <summary>
        /// Adds or Changes scripts and other components to make the entity a first person controller.
        /// </summary>
        /// <param name="entity">entity to manipulate</param>
        public static void BecomeFirstPersonController(Entity entity)
        {
            ConfigureController(entity, 4, 6, true, true, false, 2);
        }

        /// <summary>
        /// Adds or Changes scripts and other components to make the entity a fly through controller.
        /// </summary>
        /// <param name="entity">entity to manipulate</param>
        public static void BecomeFlyThroughController(Entity entity)
        {
            ConfigureController(entity, 6, 9, false, false, true, 0);
        }

        /// <summary>
        /// Enables or disables the input reactions of given entity.
        /// </summary>
        /// <param name="entity">entity to manipulate</param>
        /// <param name="enable">true or false</param>
        public static void EnableInputScripts(Entity entity, bool enable)
        {
            var scripts = entity.Get<ScriptCollection<InputScript>>();
            if (scripts == null)
                return;

            foreach (var script in scripts)
                script.Enabled = enable;
        }

        #endregion

        #region Helper Methods

        private static void ConfigureController(Entity entity, float acceleration, float maxSpeed,
            bool alligntoGround, bool affectedByGravity, bool penetratable, float friction)
        {
            if (!entity.Has<ScriptCollection<InputScript>>()) entity.AddComponent(new ScriptCollection<InputScript>());
            if (!entity.Has<Collider>()) entity.AddComponent(new Collider());
            if (!entity.Has<Surface>()) entity.AddComponent(new Surface());

            entity.Get<Collider>().IsAffectedByGravity = affectedByGravity;
            entity.Get<Collider>().IsPenetratable = penetratable;
            entity.Get<Collider>().IsInputControlled = true;
            entity.Get<Collider>().IsDynamic = true;
            entity.Get<Surface>().KineticFriction = friction;
            entity.Get<ScriptCollection<InputScript>>().AddSingle(new MousePivoting());
            entity.Get<ScriptCollection<InputScript>>().AddSingle(
                new WASDMovement(acceleration, maxSpeed, alligntoGround, !affectedByGravity));
        }

        #endregion
    }
}
