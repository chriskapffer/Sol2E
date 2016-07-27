using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using BEPUphysics;
using BEPUphysics.Collidables;
using BEPUphysics.Collidables.MobileCollidables;
using BEPUphysics.CollisionRuleManagement;
using BEPUphysics.CollisionShapes;
using BEPUphysics.CollisionShapes.ConvexShapes;
using BEPUphysics.MathExtensions;
using BEPUphysics.NarrowPhaseSystems.Pairs;
using BEPUphysics.PositionUpdating;
using BEPUphysics.Settings;
using Microsoft.Xna.Framework;
using Sol2E.Common;
using Sol2E.Core;

using BepuEntity = BEPUphysics.Entities.Entity;

using S2EModelMesh = Sol2E.Common.ModelMesh;
using S2ESimpleMesh = Sol2E.Common.SimpleMesh;

using XnaModel = Microsoft.Xna.Framework.Graphics.Model;
using XnaModelMesh = Microsoft.Xna.Framework.Graphics.ModelMesh;
using XnaModelMeshPart = Microsoft.Xna.Framework.Graphics.ModelMeshPart;

namespace Sol2E.Physics
{
    /// <summary>
    /// The physics system.
    /// For general explanatino on what a system does, see the documentation of IDomainSystem.
    /// Components used by this system are Transform, Movement, Mesh, Collider, Surface, Environment and
    /// ScriptCollection&lt;CollisionScript&gt;.
    /// 
    /// The system internally uses the BEPUphysics library for simulating the behavior
    /// of colliding meshes.
    /// </summary>
    public class PhysicsSystem : AbstractDomainSystem
    {
        #region Properties and Fields

        // used to recalculate vertex positions to account for bepu's collision margin
        // CAUTION! Not in use anymore, but kept in case its needed again some time.
        protected const float MarginAdjustmentFactor = 0.0375f;

        // bepu's physics space (the heart of the simulation)
        protected readonly Space Space;
        // a map of entity ids to colliding elements of simulation
        protected readonly IDictionary<int, BepuEntity> Bodies;
        // collection of prototypes to speed up body creation
        protected readonly ICollection<Tuple<Vector3[], BepuEntity>> Prototypes;

        // max number of iterations to solve the simulation's constraints (default is 10)
        public int IterationLimit
        {
            get { return Space.Solver.IterationLimit; }
            set { Space.Solver.IterationLimit = value; }
        }

        // max number of timesteps to perform during update (default is 3)
        public int MaxStepsPerFrame
        {
            get { return Space.TimeStepSettings.MaximumTimeStepsPerFrame; }
            set { Space.TimeStepSettings.MaximumTimeStepsPerFrame = value; }
        }

        // margin between colliding bodies (default is 0.04f)
        public float CollisionMargin
        {
            get { return CollisionDetectionSettings.DefaultMargin; }
            set { CollisionDetectionSettings.DefaultMargin = value; }
        }

        public bool IsPaused { get; set; }

        #endregion

        public PhysicsSystem()
            : base("Physics")
        {
            CollisionMargin = 0.02f;

            Space = new Space();
            Bodies = new ConcurrentDictionary<int, BepuEntity>();
            Prototypes = new List<Tuple<Vector3[], BepuEntity>>();

            IsPaused = false;

            if (System.Environment.ProcessorCount > 1)
            {
                for (int i = 0; i < System.Environment.ProcessorCount; i++)
                {
                    Space.ThreadManager.AddThread();
                }
            }
        }

        #region Implementation of AbstractDomainSystem

        /// <summary>
        /// Initializes internal resources, which might be not available at creation.
        /// </summary>
        public override void Initialize()
        {
            // register to changed events of these components, to handle them appropriately
            ComponentChangedEvent<Transform>.ComponentChanged += TransformChanged;
            ComponentChangedEvent<Movement>.ComponentChanged += MovementChanged;
            ComponentChangedEvent<Collider>.ComponentChanged += ColliderChanged;
            ComponentChangedEvent<Surface>.ComponentChanged += SurfaceChanged;
            ComponentChangedEvent<Environment>.ComponentChanged += EnvironmentChanged;
            ComponentChangedEvent<ScriptCollection<CollisionScript>>.ComponentChanged += ScriptCollectionChanged;
        }

        /// <summary>
        /// Updates the physics simulation.
        /// </summary>
        /// <param name="deltaTime">elapsed game time in total seconds</param>
        protected override void Update(float deltaTime)
        {
            if (IsPaused)
                return;

            Space.Update(deltaTime);

            foreach (var pair in Bodies)
            {
                var entity = Entity.GetInstance(pair.Key);
                var body = pair.Value;

                // update transform component from simulation
                var transform = entity.Get<Transform>();
                if (transform != null)
                {
                    // temporarily stop listening for transform changed events,
                    // because we are causing them ourselves
                    ComponentChangedEvent<Transform>.ComponentChanged -= TransformChanged;
                    transform.World = body.WorldTransform;
                    ComponentChangedEvent<Transform>.ComponentChanged += TransformChanged;
                }

                // update movement component from simulation
                var movement = entity.Get<Movement>();
                if (movement != null)
                {
                    // temporarily stop listening for movement changed events,
                    // because we are causing them ourselves
                    ComponentChangedEvent<Movement>.ComponentChanged -= MovementChanged;
                    movement.LinearVelocity = body.LinearVelocity;
                    movement.LinearMomentum = body.LinearMomentum;
                    movement.AngularVelocity = body.AngularVelocity;
                    movement.AngularMomentum = body.AngularMomentum;
                    ComponentChangedEvent<Movement>.ComponentChanged += MovementChanged;
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
            // unregister from changed events of these components
            ComponentChangedEvent<Transform>.ComponentChanged -= TransformChanged;
            ComponentChangedEvent<Movement>.ComponentChanged -= MovementChanged;
            ComponentChangedEvent<Collider>.ComponentChanged -= ColliderChanged;
            ComponentChangedEvent<Surface>.ComponentChanged -= SurfaceChanged;
            ComponentChangedEvent<Environment>.ComponentChanged -= EnvironmentChanged;
            ComponentChangedEvent<ScriptCollection<CollisionScript>>.ComponentChanged -= ScriptCollectionChanged;

            Prototypes.Clear();
            Bodies.Clear();
            Space.Dispose();
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
                    if (Bodies.ContainsKey(sender.Id))
                    {
                        if (component is Transform)
                            TransformChanged(component as Transform, "All", null);
                        else if (component is Movement)
                            MovementChanged(component as Movement, "All", null);
                        else if (component is Surface)
                            SurfaceChanged(component as Surface, "All", null);
                        else if (component is Collider)
                            ColliderChanged(component as Collider, "All", null);
                        else if (component is ScriptCollection<CollisionScript>)
                            ScriptCollectionChanged(component as ScriptCollection<CollisionScript>, "All", null);
                        else if (component is Environment)
                            EnvironmentChanged(component as Environment, "All", null);
                    }
                    break;
            }
        }

        #endregion

        #region Event Handling for Component Changes

        /// <summary>
        /// Handles changes of a Transform component.
        /// </summary>
        /// <param name="sender">component of type Transform</param>
        /// <param name="propertyName">name of property that changed</param>
        /// <param name="oldValue">old value of that property or null, if not specified</param>
        protected virtual void TransformChanged(Transform sender, string propertyName, object oldValue)
        {
            Entity entity = sender.GetHostingEntity();
            if (entity == null)
                return;

            // update physics body, if entity is used by the system
            BepuEntity body;
            if (!Bodies.TryGetValue(entity.Id, out body))
                return;

            body.Position = sender.Position;
            body.Orientation = sender.Orientation;
        }

        /// <summary>
        /// Handles changes of a Movement component.
        /// </summary>
        /// <param name="sender">component of type Movement</param>
        /// <param name="propertyName">name of property that changed</param>
        /// <param name="oldValue">old value of that property or null, if not specified</param>
        protected virtual void MovementChanged(Movement sender, string propertyName, object oldValue)
        {
            // update physics body, if entity is used by the system
            BepuEntity body;
            if (!Bodies.TryGetValue(sender.GetHostingEntity().Id, out body))
                return;

            body.LinearVelocity = sender.LinearVelocity;
            body.LinearMomentum = sender.LinearMomentum;
            body.AngularVelocity = sender.AngularVelocity;
            body.AngularMomentum = sender.AngularMomentum;
        }

        /// <summary>
        /// Handles changes of a Surface component.
        /// </summary>
        /// <param name="sender">component of type Surface</param>
        /// <param name="propertyName">name of property that changed</param>
        /// <param name="oldValue">old value of that property or null, if not specified</param>
        protected virtual void SurfaceChanged(Surface sender, string propertyName, object oldValue)
        {
            // update physics body, if entity is used by the system
            BepuEntity body;
            if (!Bodies.TryGetValue(sender.GetHostingEntity().Id, out body))
                return;

            body.Material.Bounciness = sender.Bounciness;
            body.Material.KineticFriction = sender.KineticFriction;
            body.Material.StaticFriction = sender.StaticFriction;
        }

        /// <summary>
        /// Handles changes of a Collider component.
        /// </summary>
        /// <param name="sender">component of type Collider</param>
        /// <param name="propertyName">name of property that changed</param>
        /// <param name="oldValue">old value of that property or null, if not specified</param>
        protected virtual void ColliderChanged(Collider sender, string propertyName, object oldValue)
        {
            // update physics body, if entity is used by the system
            BepuEntity body;
            if (!Bodies.TryGetValue(sender.GetHostingEntity().Id, out body))
                return;

            switch (propertyName)
            {
                case "IsDynamic":
                case "IsKinematic":
                    if (body.IsDynamic != sender.IsDynamic)
                    {
                        if (sender.IsDynamic)
                            body.BecomeDynamic(sender.Mass);
                        else
                            body.BecomeKinematic();
                    }
                    break;
                case "IsPenetratable":
                    body.CollisionInformation.CollisionRules.Personal
                        = sender.IsPenetratable ? CollisionRule.NoSolver : CollisionRule.Defer;
                    break;
                case "IsAffectedByGravity":
                    body.IsAffectedByGravity = sender.IsAffectedByGravity;
                    break;
                case "Mass":
                    body.Mass = sender.Mass;
                    break;
                case "IsInputControlled":
                    if (sender.IsInputControlled)
                    {
                        ((ConvexShape)body.CollisionInformation.Shape).CollisionMargin = .1f;
                        body.PositionUpdateMode = PositionUpdateMode.Continuous;
                        body.LocalInertiaTensorInverse = new Matrix3X3();
                    }
                    else
                    {
                        ((ConvexShape)body.CollisionInformation.Shape).CollisionMargin = CollisionMargin;
                        body.PositionUpdateMode = PositionUpdateMode.Discrete;
                    }
                    break;
                case "All":
                    // update all properties
                    if (body.IsDynamic != sender.IsDynamic)
                    {
                        if (sender.IsDynamic)
                            body.BecomeDynamic(sender.Mass);
                        else
                            body.BecomeKinematic();
                    }
                    if (sender.IsInputControlled)
                    {
                        if (body.CollisionInformation.Shape is ConvexShape)
                            ((ConvexShape)body.CollisionInformation.Shape).CollisionMargin = .1f;
                        body.PositionUpdateMode = PositionUpdateMode.Continuous;
                        body.LocalInertiaTensorInverse = new Matrix3X3();
                    }
                    body.IsAffectedByGravity = sender.IsAffectedByGravity;
                    body.CollisionInformation.CollisionRules.Personal
                        = sender.IsPenetratable ? CollisionRule.NoSolver : CollisionRule.Defer;
                    break;
            }
        }

        /// <summary>
        /// Handles changes of a ScriptCollection&lt;CollisionScript&gt; component.
        /// </summary>
        /// <param name="sender">component of type ScriptCollection&lt;CollisionScript&gt;</param>
        /// <param name="propertyName">name of property that changed</param>
        /// <param name="oldValue">old value of that property or null, if not specified</param>
        protected virtual void ScriptCollectionChanged(ScriptCollection<CollisionScript> sender, string propertyName, object oldValue)
        {
            // add or remove collision events from physics body, if entity is used by the system
            BepuEntity body;
            if (!Bodies.TryGetValue(sender.GetHostingEntity().Id, out body))
                return;

            // add collision events if we have one ore more listeners
            if ((propertyName == "Added" || propertyName == "All") && sender.ItemCount > 0)
            {
                body.CollisionInformation.Events.InitialCollisionDetected
                    += BepuEventInitialCollisionDetected;
                body.CollisionInformation.Events.CollisionEnded
                    += BepuEventCollisionEnded;
                body.CollisionInformation.Events.PairTouched
                    += BepuEventPairTouched;
            }
            // remove collision events if no one listens for them any more
            else if ((propertyName == "Removed" || propertyName == "All") && sender.ItemCount == 0)
            {
                body.CollisionInformation.Events.InitialCollisionDetected
                    -= BepuEventInitialCollisionDetected;
                body.CollisionInformation.Events.CollisionEnded
                    -= BepuEventCollisionEnded;
                body.CollisionInformation.Events.PairTouched
                    -= BepuEventPairTouched;
            }
        }

        /// <summary>
        /// Handles changes of a Environment component.
        /// </summary>
        /// <param name="sender">component of type Environment</param>
        /// <param name="propertyName">name of property that changed</param>
        /// <param name="oldValue">old value of that property or null, if not specified</param>
        protected virtual void EnvironmentChanged(Environment sender, string propertyName, object oldValue)
        {
            // update physics environment
            Space.ForceUpdater.Gravity = sender.Gravity;
        }

        #endregion

        #region Event Handling for Bepu Collision Events

        /// <summary>
        /// Handles the InitialCollisionDetected event invoked by bepu.
        /// Forwards its arguments to InvokeScriptActionFromCollisionEvent.
        /// </summary>
        /// <param name="sender">bepu entity which collided</param>
        /// <param name="other">bepu entity with which the sender collided with</param>
        /// <param name="pair">bepu collision pair (unused)</param>
        private void BepuEventInitialCollisionDetected(EntityCollidable sender, Collidable other, CollidablePairHandler pair)
        {
            InvokeScriptActionFromCollisionEvent(sender, (EntityCollidable)other, CollisionType.CollisionBegan);
        }

        /// <summary>
        /// Handles the CollisionEnded event invoked by bepu.
        /// Forwards its arguments to InvokeScriptActionFromCollisionEvent.
        /// </summary>
        /// <param name="sender">bepu entity which collided</param>
        /// <param name="other">bepu entity with which the sender collided with</param>
        /// <param name="pair">bepu collision pair (unused)</param>
        private void BepuEventCollisionEnded(EntityCollidable sender, Collidable other, CollidablePairHandler pair)
        {
            InvokeScriptActionFromCollisionEvent(sender, (EntityCollidable)other, CollisionType.CollisionEnded);
        }

        /// <summary>
        /// Handles the PairTouched event invoked by bepu.
        /// Forwards its arguments to InvokeScriptActionFromCollisionEvent.
        /// </summary>
        /// <param name="sender">bepu entity which collided</param>
        /// <param name="other">bepu entity with which the sender collided with</param>
        /// <param name="pair">bepu collision pair (unused)</param>
        private void BepuEventPairTouched(EntityCollidable sender, Collidable other, CollidablePairHandler pair)
        {
            InvokeScriptActionFromCollisionEvent(sender, (EntityCollidable)other, CollisionType.IsColliding);
        }

        /// <summary>
        /// This invokes the action of all collision scripts with the given parameters
        /// </summary>
        /// <param name="sender">bepu entity which collided</param>
        /// <param name="other">bepu entity with which the sender collided with</param>
        /// <param name="collisionType">either CollisionBegan, CollisionEnded or IsColliding</param>
        protected virtual void InvokeScriptActionFromCollisionEvent(EntityCollidable sender, EntityCollidable other, CollisionType collisionType)
        {
            Entity entity = Entity.GetInstance((int)sender.Entity.Tag);
            var scripts = entity.Get<ScriptCollection<CollisionScript>>();
            if (scripts != null)
            {
                foreach (CollisionScript script in scripts)
                    script.Action.Invoke(entity, Entity.GetInstance((int)other.Entity.Tag), collisionType);
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
            // if entity contains environment information, set it up
            var environment = entity.Get<Environment>();
            if (environment != null)
            {
                Space.ForceUpdater.Gravity = environment.Gravity;
            }

            // if entity contains a collider, create a body from its mesh and add it to list
            var collider = entity.Get<Collider>();
            if (collider != null)
            {
                // do nothing if entity has a collider but does not have a mesh
                var mesh = entity.Get<S2ESimpleMesh>() ?? (Mesh)entity.Get<S2EModelMesh>();
                if (mesh == null)
                    return;

                // if mesh is a model mesh and if it is not initialized do so
                var modelMesh = mesh as ModelMesh;
                if (modelMesh != null && !modelMesh.IsInitialized)
                {
                    modelMesh.Initialize(CurrentResourceManager.Load<XnaModel>(modelMesh.ModelName));
                    mesh = modelMesh;
                }

                // create a physical body, initialize it from given entity and add it to space and dictionary
                var body = CreateBodyFromMesh(mesh, collider.BePrecise);
                SetupBodyFromEntity(body, entity);

                Bodies.Add(entity.Id, body);
                Space.Add(body);
            }
        }

        /// <summary>
        /// Removes all resources associated with this entity from the system.
        /// </summary>
        /// <param name="entity">entity which will be removed</param>
        protected virtual void RemoveEntityResources(Entity entity)
        {
            // if the entity to be removed is used by the system remove its resources
            BepuEntity body;
            if (!Bodies.TryGetValue(entity.Id, out body))
                return;

            if (body.Space == Space)
                Space.Remove(body);

            Bodies.Remove(entity.Id);
        }

        #endregion

        /// <summary>
        /// Creates a new body from given mesh. If possible, it uses stored prototypes to
        /// speed up initialization process.
        /// </summary>
        /// <param name="mesh">mesh from which the body will be created.</param>
        /// <param name="bePrecise">indicates of compound shapes should be used for models</param>
        /// <returns>new body</returns>
        protected virtual BepuEntity CreateBodyFromMesh(Mesh mesh, bool bePrecise)
        {
            BepuEntity prototypeBody;
            var shortArray = mesh.VertexArray.Take(Math.Min(mesh.VertexArray.Length, 100));
            var prototype = Prototypes.SingleOrDefault(p => p.Item1.SequenceEqual(shortArray));
            if (prototype == null)
            {
                Vector3 center;
                var shape = (mesh is S2EModelMesh)
                    ? GetShapeFromModel(mesh as S2EModelMesh, bePrecise, out center)
                    : GetShapeFromVertexArray(mesh.VertexArray, out center);
                prototypeBody = new BEPUphysics.Entities.Entity(shape, 2)
                    {CollisionInformation = { LocalPosition = center }};
                Prototypes.Add(new Tuple<Vector3[], BepuEntity>(shortArray.ToArray(), prototypeBody));
            }
            else
            {
                prototypeBody = prototype.Item2;
            }

            return new BepuEntity(prototypeBody.CollisionInformation.Shape,
                prototypeBody.Mass, prototypeBody.LocalInertiaTensor, prototypeBody.Volume);
        }

        /// <summary>
        /// Sets up all properties of a physical body from given entity.
        /// </summary>
        /// <param name="body">body to initialize</param>
        /// <param name="entity">entity to get data from</param>
        protected virtual void SetupBodyFromEntity(BepuEntity body, Entity entity)
        {
            // assign id
            body.Tag = entity.Id;

            // if collider information are present use them for setup
            var collider = entity.Get<Collider>();
            if (collider != null)
            {
                if (collider.IsKinematic)
                    body.BecomeKinematic();
                else
                    body.Mass = collider.Mass;

                body.IsAffectedByGravity = collider.IsAffectedByGravity;

                if (collider.IsPenetratable)
                    body.CollisionInformation.CollisionRules.Personal = CollisionRule.NoSolver;

                if (collider.IsInputControlled)
                {
                    ((ConvexShape)body.CollisionInformation.Shape).CollisionMargin = .1f;
                    body.PositionUpdateMode = PositionUpdateMode.Continuous;
                    body.LocalInertiaTensorInverse = new Matrix3X3();
                }
            }

            // if surface information are present use them for setup
            var surface = entity.Get<Surface>();
            if (surface != null)
            {
                body.Material.Bounciness = surface.Bounciness;
                body.Material.KineticFriction = surface.KineticFriction;
                body.Material.StaticFriction = surface.StaticFriction;
            }

            // if transform information are present use them for setup
            var transform = entity.Get<Transform>();
            if (transform != null)
            {
                body.Position = transform.Position;
                body.Orientation = transform.Orientation;
            }

            // if movement information are present use them for setup
            var movement = entity.Get<Movement>();
            if (movement != null)
            {
                body.LinearVelocity = movement.LinearVelocity;
                body.LinearMomentum = movement.LinearMomentum;
                body.AngularVelocity = movement.AngularVelocity;
                body.AngularMomentum = movement.AngularMomentum;
            }

            // if collision scripts are present add collision event listeners to the body
            var scripts = entity.Get<ScriptCollection<CollisionScript>>();
            if (scripts != null && scripts.ItemCount > 0)
            {
                body.CollisionInformation.Events.InitialCollisionDetected
                    += BepuEventInitialCollisionDetected;
                body.CollisionInformation.Events.CollisionEnded
                    += BepuEventCollisionEnded;
                body.CollisionInformation.Events.PairTouched
                    += BepuEventPairTouched;
            }
        }

        /// <summary>
        /// Returns a convex hull shape from given vertex array
        /// </summary>
        /// <param name="vertices">vertex array</param>
        /// <param name="center">center of mass (out)</param>
        /// <returns>a convex hull shape</returns>
        private ConvexHullShape GetShapeFromVertexArray(Vector3[] vertices, out Vector3 center)
        {
            return new ConvexHullShape(vertices, out center) { CollisionMargin = CollisionMargin };
        }

        /// <summary>
        /// Returns a compound shape from given model
        /// </summary>
        /// <param name="mesh">s2e mesh containing the model's assetname</param>
        /// <param name="bePrecise">indicates of compound shapes should be used for models</param>
        /// <param name="center">center of mass (out)</param>
        /// <returns>a compound shape</returns>
        protected virtual EntityShape GetShapeFromModel(S2EModelMesh mesh, bool bePrecise, out Vector3 center)
        {
            if (!bePrecise)
            {
                var vertices = SimpleMeshFactory.CreateCylinder(1, 0.5f).VertexArray;
                BoundingBox box = Mesh.BoundingBoxFromVertexArray(mesh.VertexArray);
                Matrix scale = Matrix.CreateScale(box.Max);
                Vector3.Transform(vertices, ref scale, vertices);
                return GetShapeFromVertexArray(vertices, out center);
            }

            var subShapes = new List<CompoundShapeEntry>();

            var model = CurrentResourceManager.Load<XnaModel>(mesh.ModelName);
            var transforms = new Matrix[model.Bones.Count];
            var verticesList = new List<Vector3>();
            var dummy = new List<ushort>();

            model.CopyAbsoluteBoneTransformsTo(transforms);

            foreach (XnaModelMesh modelMesh in model.Meshes)
            {
                Matrix transform = (modelMesh.ParentBone != null
                                        ? transforms[modelMesh.ParentBone.Index]
                                        : Matrix.Identity)
                                   *mesh.Scale;

                S2EModelMesh.GetVerticesAndIndicesFromModelMesh(modelMesh, transform, verticesList, dummy);
                subShapes.Add(new CompoundShapeEntry(GetShapeFromVertexArray(verticesList.ToArray(), out center)));
                verticesList.Clear();
            }

            return new CompoundShape(subShapes, out center);
        }

        /// <summary>
        /// Adjusts all elements of a vertex array, to account for bepus collision margin.
        /// This is necessary to match the collision mesh to the mesh which will be rendered.
        /// Otherwise the game object will appear as floating around each other.
        /// 
        /// CAUTION! Not in use anymore, but kept in case its needed again some time.
        /// </summary>
        /// <param name="vertices">array of vertices, whose lengths will be changed</param>
        /// <returns>adjusted vertex array</returns>
        protected virtual Vector3[] CollisionMarginAdjustedVertices(Vector3[] vertices)
        {
            var result = new Vector3[vertices.Length];

            for (int i = 0; i < vertices.Length; i++)
            {
                // rescale the vertex with the MarginAdjustmentFactor
                // note that the overall scale factor increases with bigger length
                float length = vertices[i].Length();
                result[i] = vertices[i] * (length / (length + MarginAdjustmentFactor));
            }

            return result;
        }

        #endregion
    }

    public static class ListExtension
    {
        public static IList<T> Perforate<T>(this IList<T> collection)
        {
            for (var i = collection.Count - 1; i >= 0; i--)
                if(i % 2 == 0)
                    collection.RemoveAt(i);
            return collection;
        }
    }
}
