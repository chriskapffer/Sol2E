using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Sol2E.Common;
using Sol2E.Core;

using ModelMesh = Sol2E.Common.ModelMesh;

namespace Sol2E.Graphics
{
    /// <summary>
    /// Not a component! This is a helper class to store managed resources used by graphics system.
    /// Depending on the usage it contains an instance of a xna model or an vertex and index buffer.
    /// </summary>
    public class MeshRenderer
    {
        public bool IsModel { get; private set; }

        public Model Model { get; private set; }
        public VertexBuffer VertexBuffer { get; private set; }
        public IndexBuffer IndexBuffer { get; private set; }

        /// <summary>
        /// If created from a ModelMesh, the provided resource manager will load the model
        /// </summary>
        /// <param name="mesh">s2e model mesh containing the model's asset name</param>
        /// <param name="resourceManager">resource manager used to load the model</param>
        public MeshRenderer(ModelMesh mesh, IResourceManager resourceManager)
        {
            Model = resourceManager.Load<Model>(mesh.ModelName);
            Model.Root.Transform = mesh.LocalTransform;
            // if not set before, set the mesh's vertices from model
            if (!mesh.IsInitialized)
                mesh.Initialize(Model);

            // mark renderer for rendering models
            IsModel = true;
        }

        /// <summary>
        /// If created from a SimpleMesh, the graphics device will fill the vertex and index buffer
        /// </summary>
        /// <param name="mesh">s2e simple mesh containing everthing to set up a VertexPositionNormalTexture array</param>
        /// <param name="graphicsDevice">graphics device to create the vertex and index buffer from</param>
        public MeshRenderer(SimpleMesh mesh, GraphicsDevice graphicsDevice)
        {
            var indices = mesh.IndexArray;
            var vertices = new VertexPositionNormalTexture[mesh.VertexArray.Length];
            
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = new VertexPositionNormalTexture(
                    mesh.VertexArray[i],
                    mesh.NormalArray[i],
                    mesh.TextureArray[i]);
            }

            VertexBuffer = new VertexBuffer(graphicsDevice,
                                            typeof(VertexPositionNormalTexture), 
                                            vertices.Length, 
                                            BufferUsage.None);
            VertexBuffer.SetData(vertices);

            IndexBuffer = new IndexBuffer(graphicsDevice,
                                          typeof(ushort),
                                          indices.Length,
                                          BufferUsage.None);
            IndexBuffer.SetData(indices);

            // mark the renderer for rendering basic shapes
            IsModel = false;
        }
    }
}