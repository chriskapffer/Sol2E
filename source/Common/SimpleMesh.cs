using System;
using Microsoft.Xna.Framework;

namespace Sol2E.Common
{
    /// <summary>
    /// Component which contains a normal and texture array to be able to setup
    /// a VertexPositionNormalTexture declaration. Derived from Mesh.
    /// </summary>
    [Serializable]
    public class SimpleMesh : Mesh
    {
        public Vector3[] NormalArray { get; private set; }
        public Vector2[] TextureArray { get; private set; }

        public SimpleMesh(Vector3[] vertexArray, Vector3[] normalArray, Vector2[] textureArray, ushort[] indexArray)
        {
            Initialize(vertexArray, indexArray);

            NormalArray = normalArray;
            TextureArray = textureArray;
        }
    }
}
