using System;
using Sol2E.Common;
using Sol2E.Core;

namespace Sol2E.Physics
{
    public enum CollisionType
    {
        CollisionBegan,
        CollisionEnded,
        IsColliding
    }

    /// <summary>
    /// Abstract script, which gets invoked by collision events.
    /// </summary>
    [Serializable]
    public abstract class CollisionScript : ScriptCollectionItem
    {
        public Action<Entity, Entity, CollisionType> Action { get; private set; }

        protected CollisionScript()
        {
            Action = OnCollision;
        }

        public abstract void OnCollision(Entity sender, Entity other, CollisionType collisionType);
    }
}
