using System;
using System.Collections.Generic;
using Sol2E;
using Sol2E.Core;
using Sol2E.Input;
using Sol2E.Common;
using Sol2E.Graphics;
using Microsoft.Xna.Framework;
using Sol2E.Physics;
using Sol2E.Audio;
using Sol2E.Graphics.UI;

namespace $safeprojectname$
{
    #region Input Scripts

    [Serializable]
    public class ExitOnKeyPress : InputScript
    {
        public ExitOnKeyPress(InputSource key)
        {
            Conditions.Add(new SimpleInputCondition(key, InputState.Pressed));
        }

        public override void OnInput(Entity sender, InputSource source, InputState state, float deltaTime, object value)
        {
            VitalGameMethods.Exit();
        }
    }

    [Serializable]
    public class GenericKeyPressScript : InputScript
    {
        private readonly Action _action;

        public GenericKeyPressScript(InputSource key, Action action)
        {
            _action = action;
            Conditions.Add(new SimpleInputCondition(key, InputState.Pressed));
        }

        public override void OnInput(Entity sender, InputSource source, InputState state, float deltaTime, object value)
        {
            _action.Invoke();
        }
    }

    [Serializable]
    public class ShootBall : InputScript
    {
        public float Impulse { get; set; }
        public IList<int> ExistingBalls { get; private set; }
        public int MaxExistingBalls { get; private set; }

        public ShootBall(int maxNumberOfBalls)
        {
            Impulse = 20;
            ExistingBalls = new List<int>();
            MaxExistingBalls = maxNumberOfBalls;
            Conditions.Add(new SimpleInputCondition(InputSource.MouseLeft, InputState.Pressed));

            // add a bullet to the global scene. this is a bit hacky but enables
            // resource loading on start up, so that it is already cached for later use. 
            Scene.Global.AddEntity(CreateBullet());
        }

        public override void OnInput(Entity sender, InputSource source, InputState state, float deltaTime, object value)
        {
            var t = sender.Get<Transform>();

            Entity bullet = CreateBullet();
            bullet.Get<Movement>().LinearMomentum = (t.Forward + t.Up * 0.1f) * Impulse;
            bullet.Get<Transform>().Position = t.Position + (t.Forward * 0.3f);
            bullet.AssignToScene(Scene.Current);

            ExistingBalls.Add(bullet.Id);
            if (ExistingBalls.Count > MaxExistingBalls)
            {
                Entity.Destroy(Entity.GetInstance(ExistingBalls[0]));
                ExistingBalls.RemoveAt(0);
            }
        }

        private static Entity CreateBullet()
        {
            var bullet = Entity.Create();
            bullet.AddComponent(SimpleMeshFactory.CreateSphere(0.1f));
            bullet.AddComponent(new ScriptCollection<CollisionScript>(new BulletCollisionScript()));
            bullet.AddComponent(new Appearance { DiffuseColor = Color.Gray, SpecularColor = new Color(20, 20, 20) });
            bullet.AddComponent(new Collider { IsDynamic = true, Mass = 1 });
            bullet.AddComponent(new Transform { Position = Vector3.Down * 100 });
            bullet.AddComponent(new AudioSource("sounds/hit2") { Is3D = true, Volume = 1 } );
            bullet.AddComponent(new Movement());
            return bullet;
        }
    }

    [Serializable]
    public class WASDMovement : InputScript
    {
        public bool PositionUpdate { get; set; }
        public bool AllignToGround { get; set; }
        public float Acceleration { get; set; }
        public float MaxSpeed { get; set; }

        public WASDMovement(float acceleration, float maxSpeed, bool allignToGround = false, bool positionUpdate = false)
        {
            PositionUpdate = positionUpdate;
            AllignToGround = allignToGround;
            Acceleration = acceleration;
            MaxSpeed = maxSpeed;
            Conditions.Add(new SimpleInputCondition(InputSource.KeyW, InputState.Down));
            Conditions.Add(new SimpleInputCondition(InputSource.KeyA, InputState.Down));
            Conditions.Add(new SimpleInputCondition(InputSource.KeyS, InputState.Down));
            Conditions.Add(new SimpleInputCondition(InputSource.KeyD, InputState.Down));
        }

        public override void OnInput(Entity sender, InputSource source, InputState state, float deltaTime, object value)
        {
            var transform = sender.Get<Transform>();
            var movement = sender.Get<Movement>();
            var direction = Vector3.Zero;

            if (source == InputSource.KeyW)
                direction = transform.Forward;
            if (source == InputSource.KeyA)
                direction = transform.Left;
            if (source == InputSource.KeyS)
                direction = transform.Backward;
            if (source == InputSource.KeyD)
                direction = transform.Right;

            if (AllignToGround)
                direction.Y = 0;

            if (PositionUpdate)
            {
                transform.Position += direction.Normalized() * deltaTime * MaxSpeed;
                movement.LinearVelocity = Vector3.Zero;
                movement.LinearMomentum = Vector3.Zero;
            }
            else
            {
                Vector3 impulse = direction;
                Vector3 additionalMomentum = impulse.Normalized() * deltaTime * 10 * Acceleration;
                Vector3 newMomentum = movement.LinearMomentum + additionalMomentum;
                float speed = Math.Min(newMomentum.Length(), MaxSpeed);
                movement.LinearMomentum = newMomentum.Normalized() * speed;
            }
        }
    }

    [Serializable]
    public class MousePivoting : InputScript
    {
        public MousePivoting()
        {
            Conditions.Add(new SimpleInputCondition(InputSource.MouseMoveX, InputState.Moved));
            Conditions.Add(new SimpleInputCondition(InputSource.MouseMoveY, InputState.Moved));
        }

        public override void OnInput(Entity sender, InputSource source, InputState state, float deltaTime, object value)
        {
            float rotationSpeed = 2;

            Transform transform = sender.Get<Transform>();

            if (source == InputSource.MouseMoveX)
                transform.RotateLocalY(deltaTime * rotationSpeed * -(int)value, true);
            if (source == InputSource.MouseMoveY)
                transform.RotateLocalX(deltaTime * rotationSpeed * -(int)value, true);
        }
    }

    #endregion

    #region UI Scripts

    [Serializable]
    public class GenericButtonClickedScript : ButtonClickedScript
    {
        private readonly string _buttonTitle;
        private readonly bool _playSound;
        private readonly Action _action;

        public GenericButtonClickedScript(string buttonTitle, Action action, bool playSound = false)
        {
            _buttonTitle = buttonTitle;
            _playSound = playSound;
            _action = action;
        }

        public override void OnButtonClicked(UIButton sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(sender.Title) || sender.Title != _buttonTitle)
                return;

            if (_playSound)
            {
                var owner = Component.GetInstance(IdOfHostingComponent).GetHostingEntity();
                if (owner.Has<AudioSource>())
                    owner.Get<AudioSource>().Play();
            }

            _action.Invoke();
        }
    }

    #endregion

    #region Collision Scripts

    [Serializable]
    public class ShipCollisionScript : CollisionScript
    {
        private bool _firstImpactOnGround = true;

        public override void OnCollision(Entity sender, Entity other, CollisionType collisionType)
        {
            if (collisionType != CollisionType.CollisionBegan)
                return;

            var collider = sender.Get<Collider>();

            // if it is still in the air
            if ( !collider.IsAffectedByGravity)
            {
                // make it fall, but not if we bump into it
                if (other.Id != GameEntities.PlayerId)
                    collider.IsAffectedByGravity = true;
            }
            else
            {
                if (!_firstImpactOnGround)
                    return;

                var info = other.Get<EntityInfo>();
                if (info != null && info.Name == "ground")
                {
                    _firstImpactOnGround = false;
                    GameLevels.ScoreLevelOne.Current++;

                    if(GameLevels.ScoreLevelOne.Current == GameLevels.ScoreLevelOne.Target)
                    {
                        // play victory sound
                        var victoryEntity = Entity.GetInstance(GameLevels.VictoryEntityId);
                        victoryEntity.Get<AudioSource>().Play();
                        GameMenu.ToggleMainMenu();
                    }
                    else
                    {
                        // play an impact sound, the first time it lands on the ground
                        var audioSource = sender.Get<AudioSource>();
                        if (audioSource != null)
                            audioSource.Play();
                    }
                }
            }
        }
    }

    [Serializable]
    public class BulletCollisionScript : CollisionScript
    {
        public override void OnCollision(Entity sender, Entity other, CollisionType collisionType)
        {
            if (collisionType != CollisionType.CollisionBegan)
                return;

            var info = other.Get<EntityInfo>();
            if (info != null && info.Name != "ground")
            {
                // play a hit sound
                var audioSource = sender.Get<AudioSource>();
                if (audioSource != null)
                    audioSource.Play();
            }
        }
    }

    #endregion

    [Serializable]
    public class PlayerResetScript : CollisionScript
    {
        public override void OnCollision(Entity sender, Entity other, CollisionType collisionType)
        {
            if (collisionType == CollisionType.CollisionBegan && other.Id == GameEntities.PlayerId)
            {
                // reset score and player position
                Entity.GetInstance(GameEntities.PlayerId).Get<Transform>().Position = Vector3.Zero;
                GameLevels.ScoreLevelOne.Current = -1;
                GameMenu.ToggleMainMenu();
            }
        }
    }
}
