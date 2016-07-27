using System;

namespace Sol2E.Core
{
    /// <summary>
    /// Class to store a scene snapshot. Contains serialized scene data (all entities
    /// assigned to this scene and their configuration) as byte array and the id of the
    /// scene to which the data belongs to.
    /// </summary>
    [Serializable]
    public class SceneState
    {
        public int SceneId { get; private set; }
        public byte[] SceneData { get; private set; }

        public SceneState(int sceneId, byte[] sceneData)
        {
            SceneId = sceneId;
            SceneData = sceneData;
        }
    }
}
