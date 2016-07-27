using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using XnaModel = Microsoft.Xna.Framework.Graphics.Model;
using XnaModelMesh = Microsoft.Xna.Framework.Graphics.ModelMesh;

namespace Sol2E.Common
{
    /// <summary>
    /// Component which contains the name of the associated xna model. Derived from Mesh.
    /// </summary>
    [Serializable]
    public class ModelMesh : Mesh
    {
        // local transformation, to mach up rendered mesh with collision shape
        public Matrix LocalTransform { get; private set; }
        // gets set at creation, used to scale the model to fit into the world
        public Matrix Scale { get; private set; }
        // name of associated model, won't change during runtime
        public string ModelName { get; private set; }
        // determines if vertex array is initialized or not
        public bool IsInitialized
        {
            get { return VertexArray != null && VertexArray.Length > 0; }
        }

        public ModelMesh(string modelName, float scale)
            : this(modelName, scale, Matrix.Identity) { }

        public ModelMesh(string modelName, float scale, Matrix localTransform)
        {
            Scale = Matrix.CreateScale(scale);
            ModelName = modelName;
            LocalTransform = localTransform;
        }

        /// <summary>
        /// Initializes vertex and index array from model.
        /// </summary>
        /// <param name="model">Xna model instance.</param>
        public void Initialize(XnaModel model)
        {
            Vector3[] vertices;
            ushort[] indices;

            GetVerticesAndIndicesFromModel(model, Scale, out vertices, out indices);

            base.Initialize(vertices, indices);
        }

        /// <summary>
        /// Gets an array of vertices and indices from the provided model.
        /// </summary>
        /// <param name="model">model to use for the collision shape</param>
        /// <param name="scale">matrix to cale model meshes, to fit into the world</param>
        /// <param name="vertices">compiled set of vertices from the model</param>
        /// <param name="indices">compiled set of indices from the model</param>
        public static void GetVerticesAndIndicesFromModel(XnaModel model, Matrix scale, out Vector3[] vertices, out ushort[] indices)
        {
            var verticesList = new List<Vector3>();
            var indicesList = new List<ushort>();
            var transforms = new Matrix[model.Bones.Count];
            model.CopyAbsoluteBoneTransformsTo(transforms);

            foreach (XnaModelMesh mesh in model.Meshes)
            {
                Matrix transform = mesh.ParentBone != null
                    ? transforms[mesh.ParentBone.Index]
                    : Matrix.Identity;
                GetVerticesAndIndicesFromModelMesh(mesh, transform * scale, verticesList, indicesList);
            }

            vertices = verticesList.ToArray();
            indices = indicesList.ToArray();
        }

        /// <summary>
        /// Adds a mesh's vertices and indices to the given lists.
        /// </summary>
        /// <param name="modelMesh">model mesh to use for the collision shape</param>
        /// <param name="transform">transform to apply to the mesh</param>
        /// <param name="vertices">list to receive vertices from the mesh</param>
        /// <param name="indices">list to receive indices from the mesh</param>
        public static void GetVerticesAndIndicesFromModelMesh(XnaModelMesh modelMesh,
            Matrix transform, List<Vector3> vertices, IList<ushort> indices)
        {
            foreach (ModelMeshPart meshPart in modelMesh.MeshParts)
            {
                GetVerticesAndIndicesFromModelMeshPart(meshPart, transform, vertices, indices);
            }
        }

        /// <summary>
        /// Adds a mesh part's vertices and indices to the given lists.
        /// </summary>
        /// <param name="meshPart">model mesh part to use for the collision shape</param>
        /// <param name="transform">transform to apply to the mesh</param>
        /// <param name="vertices">list to receive vertices from the mesh</param>
        /// <param name="indices">list to receive indices from the mesh</param>
        public static void GetVerticesAndIndicesFromModelMeshPart(ModelMeshPart meshPart,
            Matrix transform, List<Vector3> vertices, IList<ushort> indices)
        {
            var startIndex = (ushort) vertices.Count;
            var meshPartVertices = new Vector3[meshPart.NumVertices];
            //Grab position data from the mesh part.
            int stride = meshPart.VertexBuffer.VertexDeclaration.VertexStride;
            meshPart.VertexBuffer.GetData(
                meshPart.VertexOffset*stride,
                meshPartVertices,
                0,
                meshPart.NumVertices,
                stride);

            //Transform it so its vertices are located in the model's space as opposed to mesh part space.
            Vector3.Transform(meshPartVertices, ref transform, meshPartVertices);
            vertices.AddRange(meshPartVertices);

            if (meshPart.IndexBuffer.IndexElementSize == IndexElementSize.ThirtyTwoBits)
            {
                var meshIndices = new ushort[meshPart.PrimitiveCount*3];
                meshPart.IndexBuffer.GetData(meshPart.StartIndex*4, meshIndices, 0, meshPart.PrimitiveCount*3);
                foreach (ushort index in meshIndices)
                {
                    indices.Add((ushort) (startIndex + index));
                }
            }
            else
            {
                var meshIndices = new ushort[meshPart.PrimitiveCount*3];
                meshPart.IndexBuffer.GetData(meshPart.StartIndex*2, meshIndices, 0, meshPart.PrimitiveCount*3);
                foreach (ushort index in meshIndices)
                {
                    indices.Add((ushort) (startIndex + index));
                }
            }
        }
    }
}
