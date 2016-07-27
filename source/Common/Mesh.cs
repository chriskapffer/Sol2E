using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Sol2E.Core;

namespace Sol2E.Common
{
    /// <summary>
    /// Abstract component, which holds common fields for SimpelMeshes and ModelMeshes
    /// </summary>
    [Serializable]
    public abstract class Mesh : Component
    {
        public Vector3[] VertexArray { get; private set; }
        public ushort[] IndexArray { get; private set; }

        public BoundingBox BoundingBox { get; private set; }

        /// <summary>
        /// Initializes vertex and index array. Gets called by subclasses.
        /// The arrays might not be present at creation time, e.g. when they
        /// are extracted from a model at a later stage, when a scene gets
        /// loaded. The Mesh has to exist before, so can't do it inside the
        /// constructor.
        /// </summary>
        /// <param name="vertexArray">vertexArray</param>
        /// <param name="indexArray">indexArray</param>
        protected void Initialize(Vector3[] vertexArray, ushort[] indexArray)
        {
            VertexArray = vertexArray;
            IndexArray = indexArray;

            BoundingBox = BoundingBoxFromVertexArray(VertexArray);
        }

        /// <summary>
        /// Creates a BoundingBoc from a vertex array.
        /// </summary>
        /// <param name="vertexArray">vertexArray to create the box from</param>
        /// <returns>created bounding box</returns>
        public static BoundingBox BoundingBoxFromVertexArray(IEnumerable<Vector3> vertexArray)
        {
            var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            foreach(var vertex in vertexArray)
            {
                min = Vector3.Min(min, vertex);
                max = Vector3.Max(max, vertex);
            }

            return new BoundingBox(min, max);
        }
    }
}
