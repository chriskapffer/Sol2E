using System;

namespace Sol2E.Core
{
    /// <summary>
    /// Class to store an entity snapshot. Contains serialized entity data (all components
    /// assigned to this entity and their configuration) as byte array and the id of the
    /// entity to which the data belongs to.
    /// </summary>
    [Serializable]
    public class EntityState
    {
        public int EntityId { get; private set; }
        public byte[] EntityData { get; private set; }

        public EntityState(int entityId, byte[] entityData)
        {
            EntityId = entityId;
            EntityData = entityData;
        }
    }
}
