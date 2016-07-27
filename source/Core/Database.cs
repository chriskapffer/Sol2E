using System;
using System.Collections.Generic;
using System.Linq;
using Sol2E.Utils;

namespace Sol2E.Core
{
    public static class EnumerableExtension
    {
        public static bool IsSubsetOf<T>(this IEnumerable<T> a, IEnumerable<T> b)
        {
            return !a.Except(b).Any();
        }
    }

    /// <summary>
    /// The heart of the engine! The whole thing consists of three dictionaries, which each
    /// map either a scene, an entity or a component to its id. Furthermore there are three
    /// other dictionaries, which are used to speed up the access to an entity or a component.
    /// These dictionaries operate only on the ids to keep a small memory print.
    /// 
    /// All methods are solely used within this assembly, thus making this class internal.
    /// Documentation for most methods is not existent, as their names give enough information.
    /// However there are a few private helper methods, which keep all the dictionaries consistent.
    /// They are well documented.
    /// </summary>
    internal class Database : Singleton<Database>
    {
        // used to distinguish between IDPool instances
        private enum IdTypes
        {
            Scene,
            Entity,
            Component
        };

        #region Fields

        // maps scenes, entitys and components to their ids
        private readonly IDictionary<int, Scene> _scenes;
        private readonly IDictionary<int, Entity> _entities;
        private readonly IDictionary<int, Component> _components;

        // faster look up for components first by their assigned scene, then by their type
        private readonly IDictionary<int, IDictionary<Type, ISet<int>>> _componentIdsBySceneAndType;
        // faster look up for components first by their assigned entity, then by their type
        private readonly IDictionary<int, IDictionary<Type, int>> _componentIdsByEntityAndType;
        // faster look up for entities by their assigned scene
        private readonly IDictionary<int, ISet<int>> _entityIdsByScene;

        public int SceneCount
        {
            get { return _scenes.Count; }
        }
        public int EntityCount
        {
            get { return _entities.Count; }
        }
        public int ComponentCount
        {
            get { return _components.Count; }
        }

        #endregion

        private Database()
        {
            _scenes = new Dictionary<int, Scene>();
            _entities = new Dictionary<int, Entity>();
            _components = new Dictionary<int, Component>();

            _componentIdsBySceneAndType = new Dictionary<int, IDictionary<Type, ISet<int>>>();
            _componentIdsByEntityAndType = new Dictionary<int, IDictionary<Type, int>>();
            _entityIdsByScene = new Dictionary<int, ISet<int>>();
        }

        #region Lifecycle Methods

        public Scene SceneCreate()
        {
            int sceneId = IDPool.GetInstance(IdTypes.Scene).GetNextAvailableID(_scenes.Keys);
            var newScene = new Scene(sceneId);
            _scenes.Add(sceneId, newScene);
            return newScene;
        }

        public Entity EntityCreate()
        {
            int entityId = IDPool.GetInstance(IdTypes.Entity).GetNextAvailableID(_entities.Keys);
            var newEntity = new Entity(entityId);
            _entities.Add(entityId, newEntity);
            return newEntity;
        }

        public void ComponentRegister(Component component)
        {
            int componentId = IDPool.GetInstance(IdTypes.Component).GetNextAvailableID(_components.Keys);
            component.Id = componentId;
            _components.Add(componentId, component);
        }

        public void SceneDestroy(Scene scene)
        {
            foreach (Entity entity in SceneGetAllEntities(scene.Id))
            {
                EntityDestroy(entity);
            }

            _componentIdsBySceneAndType.Remove(scene.Id);
            _entityIdsByScene.Remove(scene.Id);
            _scenes.Remove(scene.Id);
        }

        public void EntityDestroy(Entity entity)
        {
            int sceneId = EntityGetIdOfHostingScene(entity.Id);

            var componentIdsOfEntity = _componentIdsByEntityAndType[entity.Id];
            foreach (int componentId in componentIdsOfEntity.Values)
            {
                _components.Remove(componentId);
            }
            _componentIdsByEntityAndType.Remove(entity.Id);

            if (sceneId != IDPool.InvalidID)
            {
                RemoveComponentIdsOfEntityFromScene(componentIdsOfEntity, sceneId);
                RemoveEntityIdFromScene(entity.Id, sceneId);
            }
            _entities.Remove(entity.Id);
        }

        public void ComponentUnRegister(Component component)
        {
            var componentType = component.GetType();
            int entityId = ComponentGetIdOfHostingEntity(componentType, component.Id);
            if (entityId != IDPool.InvalidID)
            {
                RemoveComponentFromEntity(componentType, entityId);
                int sceneId = EntityGetIdOfHostingScene(entityId);
                if (sceneId != IDPool.InvalidID)
                {
                    RemoveComponentFromScene(componentType, component.Id, sceneId);
                }
            }
            _components.Remove(component.Id);
        }

        #endregion

        #region Instance Access Methods

        public Scene SceneGetInstance(int sceneId)
        {
            Scene scene;
            _scenes.TryGetValue(sceneId, out scene);
            return scene;
        }

        public Entity EntityGetInstance(int entityId)
        {
            Entity entity;
            _entities.TryGetValue(entityId, out entity);
            return entity;
        }

        public Component ComponentGetInstance(int componentId)
        {
            Component component;
            _components.TryGetValue(componentId, out component);
            return component;
        }

        public IEnumerable<Entity> EntityGetInstances(Type componentType)
        {
            return (from entry in _componentIdsByEntityAndType
                    where entry.Value.Keys.Contains(componentType)
                    select _entities[entry.Key]);
        }

        public IEnumerable<Entity> EntityGetInstances(IEnumerable<Type> componentTypes)
        {
            return (from entry in _componentIdsByEntityAndType
                    where componentTypes.IsSubsetOf(entry.Value.Keys)
                    select _entities[entry.Key]);
        }

        public IEnumerable<Scene> SceneGetAllInstances()
        {
            return _scenes.Values;
        }

        public IEnumerable<T> ComponentGetAllInstances<T>(Type componentType) where T : Component
        {
            return _components.Values
                .Where(component => component.GetType() == componentType)
                .Select(component => component as T);
        }

        #endregion

        #region Persistency Methods

        public SceneState SceneSaveState(int sceneId)
        {
            var entityStates = SceneGetAllEntities(sceneId)
                .Select(entity => Tuple.Create(entity, EntitySaveState(entity.Id)));
            var data = Serializer.Serialize<IEnumerable<Tuple<Entity, EntityState>>>(entityStates.ToList());

            return new SceneState(sceneId, data);
        }

        public EntityState EntitySaveState(int entityId)
        {
            var componentsOfEntity = EntityGetAllComponents(entityId);
            var data = Serializer.Serialize<IEnumerable<Component>>(componentsOfEntity.ToList());

            return new EntityState(entityId, data);
        }

        public void SceneRestoreState(int sceneId, SceneState state)
        {
            var entityStates = Serializer.Deserialize<IEnumerable<Tuple<Entity, EntityState>>>(state.SceneData);

            foreach (var tuple in entityStates)
            {
                var entity = tuple.Item1;
                var entityState = tuple.Item2;

                if (_entities.ContainsKey(entity.Id))
                    _entities[entity.Id] = entity;
                else
                    _entities.Add(entity.Id, entity);

                EntityAssignToScene(entity.Id, sceneId);
                EntityRestoreState(entity.Id, entityState);
            }
        }

        public void EntityRestoreState(int entityId, EntityState state)
        {
            var componentsOfEntity = Serializer.Deserialize<IEnumerable<Component>>(state.EntityData);

            foreach (var component in componentsOfEntity)
            {
                if (_components.ContainsKey(component.Id))
                    _components[component.Id] = component;
                else
                    _components.Add(component.Id, component);

                ComponentAssignToEntity(component.GetType(), component.Id, entityId);
            }
        }

        #endregion

        #region Ownership Methods

        public void EntityAssignToScene(int entityId, int sceneId)
        {
            IDictionary<Type, int> componentIdsOfEntity;
            if (!_componentIdsByEntityAndType.TryGetValue(entityId, out componentIdsOfEntity))
            {
                componentIdsOfEntity = new Dictionary<Type, int>();
                _componentIdsByEntityAndType.Add(entityId, componentIdsOfEntity);
            }

            int oldSceneId = EntityGetIdOfHostingScene(entityId);
            if (oldSceneId != IDPool.InvalidID)
            {
                if (oldSceneId == sceneId)
                    return;

                RemoveComponentIdsOfEntityFromScene(componentIdsOfEntity, oldSceneId);
                RemoveEntityIdFromScene(entityId, oldSceneId);
            }

            AddEntityIdToScene(entityId, sceneId);
            AddComponentIdsOfEntityToScene(componentIdsOfEntity, sceneId);
        }

        public int EntityGetIdOfHostingScene(int entityId)
        {
            foreach (var entry in _entityIdsByScene)
            {
                int sceneId = entry.Key;
                var entityIdsOfScene = entry.Value;

                if (entityIdsOfScene.Contains(entityId))
                    return sceneId;
            }
            return IDPool.InvalidID;
        }

        public void ComponentAssignToEntity(Type componentType, int componentId, int entityId)
        {
            int newSceneId = EntityGetIdOfHostingScene(entityId);

            int oldEntityId = ComponentGetIdOfHostingEntity(componentType, componentId);
            if (oldEntityId != IDPool.InvalidID)
            {
                if (oldEntityId == entityId)
                    return;

                RemoveComponentFromEntity(componentType, oldEntityId);

                int oldSceneId = EntityGetIdOfHostingScene(oldEntityId);
                if (oldSceneId != newSceneId)
                {
                    RemoveComponentFromScene(componentType, componentId, oldSceneId);
                }
            }

            AddComponentToEntity(componentType, componentId, entityId);

            if (newSceneId != IDPool.InvalidID)
                AddComponentToScene(componentType, componentId, newSceneId);
        }

        public int ComponentGetIdOfHostingEntity(Type componentType, int componentId)
        {
            foreach (var entry in _componentIdsByEntityAndType)
            {
                int entityId = entry.Key;
                var componentIdsOfEntity = entry.Value;

                int componentIdOfType;
                if (componentIdsOfEntity.TryGetValue(componentType, out componentIdOfType)
                    && componentIdOfType == componentId)
                    return entityId;
            }
            return IDPool.InvalidID;
        }

        public int EntityCountInScene(Scene scene)
        {
            if (scene == null)
                return 0;

            ISet<int> entitiesOfScene;
            return _entityIdsByScene.TryGetValue(scene.Id, out entitiesOfScene) ? entitiesOfScene.Count : 0;
        }

        public int ComponentCountInScene(Scene scene)
        {
            if (scene == null)
                return 0;

            int total = 0;
            IDictionary<Type, ISet<int>> componentsOfScene;
            if (_componentIdsBySceneAndType.TryGetValue(scene.Id, out componentsOfScene))
                total += componentsOfScene.Values.Sum(componentsOfType => componentsOfType.Count);
            return total;
        }

        public int ComponentCountOfType(Type type)
        {
            int total = 0;
            foreach (var componentsOfScene in _componentIdsBySceneAndType.Values)
            {
                ISet<int> componentsOfType;
                if (componentsOfScene.TryGetValue(type, out componentsOfType))
                    total += componentsOfType.Count;
            }
            return total;
        }

        public int ComponentCountOfTypeInScene(Type type, Scene scene)
        {
            ISet<int> componentsOfType;
            IDictionary<Type, ISet<int>> componentsOfScene;
            return (_componentIdsBySceneAndType.TryGetValue(scene.Id, out componentsOfScene)
                && componentsOfScene.TryGetValue(type, out componentsOfType))
                    ? componentsOfType.Count : 0;
        }

        #endregion

        #region Data Access Methods

        public bool SceneHasEntity(int sceneId, int entityId)
        {
            ISet<int> entityIdsOfScene;
            return _entityIdsByScene.TryGetValue(sceneId, out entityIdsOfScene)
                && entityIdsOfScene.Contains(entityId);
        }

        public bool EntityHasComponent(int entityId, Type componentType)
        {
            IDictionary<Type, int> componentIdsOfEntity;
            return _componentIdsByEntityAndType.TryGetValue(entityId, out componentIdsOfEntity)
                && componentIdsOfEntity.ContainsKey(componentType);
        }

        public IEnumerable<Entity> SceneGetAllEntities(int sceneId)
        {
            ISet<int> entityIds;
            return _entityIdsByScene.TryGetValue(sceneId, out entityIds)
                ? entityIds.Select(id => _entities[id])
                : new List<Entity>();
        }

        public IEnumerable<Entity> SceneGetAllEntitiesWith(int sceneId, Type componentType)
        {
            ISet<int> entityIds;
            if (!_entityIdsByScene.TryGetValue(sceneId, out entityIds))
                return new List<Entity>();

            return entityIds
                .Where(entityId => _componentIdsByEntityAndType[entityId].ContainsKey(componentType))
                .Select(id => _entities[id]); 
        }

        public IEnumerable<Entity> SceneGetAllEntitiesWith(int sceneId, IEnumerable<Type> componentTypes)
        {
            ISet<int> entityIds;
            if(!_entityIdsByScene.TryGetValue(sceneId, out entityIds))
                return new List<Entity>();

            return entityIds
                .Where(entityId => _componentIdsByEntityAndType.ContainsKey(entityId)
                    && componentTypes.IsSubsetOf(_componentIdsByEntityAndType[entityId].Keys))
                .Select(id => _entities[id]);
        }

        public IEnumerable<T> SceneGetAllComponents<T>(int sceneId, Type componentType) where T : Component
        {
            ISet<int> componentIds;
            IDictionary<Type, ISet<int>> componentIdsOfScene;
            return _componentIdsBySceneAndType.TryGetValue(sceneId, out componentIdsOfScene)
                && componentIdsOfScene.TryGetValue(componentType, out componentIds)
                    ? componentIds.Select(id => (T) _components[id])
                    : new List<T>();
        }

        public IEnumerable<Component> EntityGetAllComponents(int entityId)
        {
            IDictionary<Type, int> componentIdsOfEntity;
            return _componentIdsByEntityAndType.TryGetValue(entityId, out componentIdsOfEntity)
                ? componentIdsOfEntity.Values.Select(id => _components[id])
                : new List<Component>();
        }

        public T EntityGetComponent<T>(int entityId, Type componentType) where T : Component
        {
            int componentId;
            IDictionary<Type, int> componentIdsOfEntity;
            return _componentIdsByEntityAndType.TryGetValue(entityId, out componentIdsOfEntity)
                && componentIdsOfEntity.TryGetValue(componentType, out componentId)
                    ? (T) _components[componentId]
                    : null;
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Adds entityId to _entityIdsByScene.
        /// sceneId is used as a key to access the set of
        /// entityIds belonging to the scene (entityIdsOfScene).
        /// </summary>
        /// <param name="entityId">entityId to add</param>
        /// <param name="sceneId">key to get entityIdsOfScene</param>
        private void AddEntityIdToScene(int entityId, int sceneId)
        {
            ISet<int> entityIdsOfScene;
            if (!_entityIdsByScene.TryGetValue(sceneId, out entityIdsOfScene))
            {
                // add a new set of entity ids for this scene,
                // if _entityIdsByScene did not contain sceneId
                entityIdsOfScene = new HashSet<int>();
                _entityIdsByScene.Add(sceneId, entityIdsOfScene);
            }
            entityIdsOfScene.Add(entityId);
        }

        /// <summary>
        /// Removes entityId to _entityIdsByScene.
        /// sceneId is used as a key to access the set of
        /// entityIds belonging to the scene (entityIdsOfScene).
        /// </summary>
        /// <param name="entityId">entityId to remove</param>
        /// <param name="sceneId">key to get entityIdsOfScene</param>
        private void RemoveEntityIdFromScene(int entityId, int sceneId)
        {
            ISet<int> entityIdsOfScene;
            if (!_entityIdsByScene.TryGetValue(sceneId, out entityIdsOfScene))
                return;

            entityIdsOfScene.Remove(entityId);

            // remove sceneId from _entityIdsByScene, if set of entity ids is empty
            if (entityIdsOfScene.Count == 0)
                _entityIdsByScene.Remove(sceneId);
        }

        /// <summary>
        /// Adds componentId to _componentIdsByEntityAndType.
        /// entityId is used as a key to access the dictionary of
        /// componentTypes as keys and componentId as values (componentIdsOfEntity).
        /// </summary>
        /// <param name="componentType">key used to add componentId</param>
        /// <param name="componentId">componentId to add</param>
        /// <param name="entityId">key to get componentIdsOfEntity</param>
        private void AddComponentToEntity(Type componentType, int componentId, int entityId)
        {
            IDictionary<Type, int> componentIdsOfEntity;
            if (!_componentIdsByEntityAndType.TryGetValue(entityId, out componentIdsOfEntity))
            {
                // add a new map of component type and ids for this entity,
                // if _componentIdsByEntityAndType did not contain entityId
                componentIdsOfEntity = new Dictionary<Type, int>();
                _componentIdsByEntityAndType.Add(entityId, componentIdsOfEntity);
            }
            componentIdsOfEntity.Add(componentType, componentId);
        }

        /// <summary>
        /// Removes the key componentType from _componentIdsByEntityAndType.
        /// entityId is used as a key to access the dictionary of
        /// componentTypes as keys and componentId as values (componentIdsOfEntity).
        /// </summary>
        /// <param name="componentType">key used to remove componentId</param>
        /// <param name="entityId">key to get componentIdsOfEntity</param>
        private void RemoveComponentFromEntity(Type componentType, int entityId)
        {
            IDictionary<Type, int> componentIdsOfEntity;
            if (!_componentIdsByEntityAndType.TryGetValue(entityId, out componentIdsOfEntity))
                return;

            componentIdsOfEntity.Remove(componentType);

            // remove entityId from _componentIdsByEntityAndType,
            // if map of component type and ids is empty
            if (componentIdsOfEntity.Count == 0)
                _componentIdsByEntityAndType.Remove(entityId);
        }

        /// <summary>
        /// Adds componentId to _componentIdsBySceneAndType.
        /// sceneId is used as a key to access the dictionary (componentIdsOfScene)
        /// of componentTypes as keys and sets of component ids as values (componentIdsOfType).
        /// </summary>
        /// <param name="componentType">key used to get componentIdsOfType</param>
        /// <param name="componentId">componentId to add</param>
        /// <param name="sceneId">key to get componentIdsOfScene</param>
        private void AddComponentToScene(Type componentType, int componentId, int sceneId)
        {
            IDictionary<Type, ISet<int>> componentIdsOfScene;
            if (!_componentIdsBySceneAndType.TryGetValue(sceneId, out componentIdsOfScene))
            {
                // add a new map of component type and ids for this scene,
                // if _componentIdsBySceneAndType did not contain sceneId
                componentIdsOfScene = new Dictionary<Type, ISet<int>>();
                _componentIdsBySceneAndType.Add(sceneId, componentIdsOfScene);
            }

            ISet<int> componentIdsOfType;
            if (!componentIdsOfScene.TryGetValue(componentType, out componentIdsOfType))
            {
                // add a new set of component ids for this type,
                // if componentIdsOfScene did not contain componentType
                componentIdsOfType = new HashSet<int>();
                componentIdsOfScene.Add(componentType, componentIdsOfType);
            }
            componentIdsOfType.Add(componentId);
        }

        /// <summary>
        /// Removes componentId from _componentIdsBySceneAndType.
        /// sceneId is used as a key to access the dictionary (componentIdsOfScene)
        /// of componentTypes as keys and sets of component ids as values (componentIdsOfType).
        /// </summary>
        /// <param name="componentType">key used to get componentIdsOfType</param>
        /// <param name="componentId">componentId to remove</param>
        /// <param name="sceneId">key to get componentIdsOfScene</param>
        private void RemoveComponentFromScene(Type componentType, int componentId, int sceneId)
        {
            IDictionary<Type, ISet<int>> componentIdsOfScene;
            if (!_componentIdsBySceneAndType.TryGetValue(sceneId, out componentIdsOfScene))
                return;

            ISet<int> componentIdsOfType;
            if (!componentIdsOfScene.TryGetValue(componentType, out componentIdsOfType))
                return;

            componentIdsOfType.Remove(componentId);

            // remove componentType from componentIdsOfScene, if set of component ids is empty
            if (componentIdsOfType.Count == 0)
                componentIdsOfScene.Remove(componentType);

            // remove sceneId from _componentIdsBySceneAndType,
            // if map of component type and ids is empty
            if (componentIdsOfScene.Count == 0)
                _componentIdsBySceneAndType.Remove(sceneId);
        }

        /// <summary>
        /// Adds all component ids of entity to _componentIdsBySceneAndType.
        /// This method iterates over all component ids in of the entity
        /// and calls AddComponentToScene each time.
        /// </summary>
        /// <param name="componentIdsOfEntity">component ids of entity</param>
        /// <param name="sceneId">key to get componentIdsOfScene</param>
        private void AddComponentIdsOfEntityToScene(IDictionary<Type, int> componentIdsOfEntity, int sceneId)
        {
            foreach (var pair in componentIdsOfEntity)
                AddComponentToScene(pair.Key, pair.Value, sceneId);
        }

        /// <summary>
        /// Removes all component ids of entity from _componentIdsBySceneAndType.
        /// This method iterates over all component ids in of the entity
        /// and calls RemoveComponentFromScene each time.
        /// </summary>
        /// <param name="componentIdsOfEntity">component ids of entity</param>
        /// <param name="sceneId">key to get componentIdsOfScene</param>
        private void RemoveComponentIdsOfEntityFromScene(IDictionary<Type, int> componentIdsOfEntity, int sceneId)
        {
            foreach (var pair in componentIdsOfEntity)
                RemoveComponentFromScene(pair.Key, pair.Value, sceneId);
        }

        #endregion
    }
}

