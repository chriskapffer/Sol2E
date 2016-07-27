using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Sol2E.Common
{
    /// <summary>
    /// A Factory to create SimpleMesh instances. Supported shapes
    /// are Cube, Sphere, Cylinder, Cone and Capsule.
    /// </summary>
    public static class SimpleMeshFactory
    {
        public const int DefaultTessellation = 24;

        public static SimpleMesh CreateCube() { return CreateCube(1f); }
        public static SimpleMesh CreateCube(float size) { return CreateCube(Vector3.One * size); }
        public static SimpleMesh CreateCube(Vector3 dimensions)
        {
            var boundingBox = new BoundingBox(
                new Vector3(-dimensions.X / 2f, -dimensions.Y / 2f, -dimensions.Z / 2f),
                new Vector3( dimensions.X / 2f,  dimensions.Y / 2f,  dimensions.Z / 2f));

            var corners = boundingBox.GetCorners();

            var textureCoords = new Vector2[4];
            textureCoords[0] = new Vector2(0, 0);
            textureCoords[1] = new Vector2(1, 0);
            textureCoords[2] = new Vector2(1, 1);
            textureCoords[3] = new Vector2(0, 1);

            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var textures = new List<Vector2>();
            var indices = new List<ushort>();

            vertices.Add(corners[0]); normals.Add(Vector3.Backward); textures.Add(textureCoords[0]);
            vertices.Add(corners[1]); normals.Add(Vector3.Backward); textures.Add(textureCoords[1]);
            vertices.Add(corners[2]); normals.Add(Vector3.Backward); textures.Add(textureCoords[2]);
            vertices.Add(corners[3]); normals.Add(Vector3.Backward); textures.Add(textureCoords[3]);
            indices.Add(0);
            indices.Add(1);
            indices.Add(2);
            indices.Add(0);
            indices.Add(2);
            indices.Add(3);

            vertices.Add(corners[1]); normals.Add(Vector3.Right); textures.Add(textureCoords[0]);
            vertices.Add(corners[2]); normals.Add(Vector3.Right); textures.Add(textureCoords[3]);
            vertices.Add(corners[5]); normals.Add(Vector3.Right); textures.Add(textureCoords[1]);
            vertices.Add(corners[6]); normals.Add(Vector3.Right); textures.Add(textureCoords[2]);
            indices.Add(4);
            indices.Add(6);
            indices.Add(7);
            indices.Add(4);
            indices.Add(7);
            indices.Add(5);

            vertices.Add(corners[4]); normals.Add(Vector3.Forward); textures.Add(textureCoords[1]);
            vertices.Add(corners[5]); normals.Add(Vector3.Forward); textures.Add(textureCoords[0]);
            vertices.Add(corners[6]); normals.Add(Vector3.Forward); textures.Add(textureCoords[3]);
            vertices.Add(corners[7]); normals.Add(Vector3.Forward); textures.Add(textureCoords[2]);
            indices.Add(9);
            indices.Add(8);
            indices.Add(11);
            indices.Add(9);
            indices.Add(11);
            indices.Add(10);

            vertices.Add(corners[0]); normals.Add(Vector3.Left); textures.Add(textureCoords[1]);
            vertices.Add(corners[3]); normals.Add(Vector3.Left); textures.Add(textureCoords[2]);
            vertices.Add(corners[4]); normals.Add(Vector3.Left); textures.Add(textureCoords[0]);
            vertices.Add(corners[7]); normals.Add(Vector3.Left); textures.Add(textureCoords[3]);
            indices.Add(14);
            indices.Add(12);
            indices.Add(13);
            indices.Add(14);
            indices.Add(13);
            indices.Add(15);

            vertices.Add(corners[0]); normals.Add(Vector3.Up); textures.Add(textureCoords[2]);
            vertices.Add(corners[1]); normals.Add(Vector3.Up); textures.Add(textureCoords[3]);
            vertices.Add(corners[4]); normals.Add(Vector3.Up); textures.Add(textureCoords[1]);
            vertices.Add(corners[5]); normals.Add(Vector3.Up); textures.Add(textureCoords[0]);
            indices.Add(16);
            indices.Add(19);
            indices.Add(17);
            indices.Add(16);
            indices.Add(18);
            indices.Add(19);

            vertices.Add(corners[2]); normals.Add(Vector3.Down); textures.Add(textureCoords[1]);
            vertices.Add(corners[3]); normals.Add(Vector3.Down); textures.Add(textureCoords[0]);
            vertices.Add(corners[6]); normals.Add(Vector3.Down); textures.Add(textureCoords[2]);
            vertices.Add(corners[7]); normals.Add(Vector3.Down); textures.Add(textureCoords[3]);
            indices.Add(21);
            indices.Add(20);
            indices.Add(22);
            indices.Add(21);
            indices.Add(22);
            indices.Add(23);

            return new SimpleMesh(vertices.ToArray(), normals.ToArray(), textures.ToArray(), indices.ToArray());
        }

        public static SimpleMesh CreateSphere() { return CreateSphere(0.5f); }
        public static SimpleMesh CreateSphere(float radius) { return CreateSphere(radius, DefaultTessellation); }
        public static SimpleMesh CreateSphere(float radius, int tessellation)
        {
            var n = new Vector3();
            float angleBetweenFacets = MathHelper.TwoPi / tessellation;

            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var textures = new List<Vector2>();
            var indices = new List<ushort>();

            //Create vertex list
            vertices.Add(new Vector3(0, radius, 0)); normals.Add(Vector3.Up); textures.Add(Vector2.Zero);
            for (int i = 1; i < tessellation / 2; i++)
            {
                float phi = MathHelper.PiOver2 - i * angleBetweenFacets;
                float sinPhi = (float)Math.Sin(phi);
                float cosPhi = (float)Math.Cos(phi);

                for (int j = 0; j < tessellation; j++)
                {
                    float theta = j * angleBetweenFacets;

                    n.X = (float)Math.Cos(theta) * cosPhi;
                    n.Y = sinPhi;
                    n.Z = (float)Math.Sin(theta) * cosPhi;

                    vertices.Add(n * radius); normals.Add(n); textures.Add(new Vector2(i * 2 / (float)(tessellation - 1), j / (float)(tessellation - 1)));
                }
            }
            vertices.Add(new Vector3(0, -radius, 0)); normals.Add(Vector3.Down); textures.Add(Vector2.Zero);

            //Create index list
            for (int i = 0; i < tessellation; i++)
            {
                indices.Add((ushort)(vertices.Count - 1));
                indices.Add((ushort)(vertices.Count - 2 - i));
                indices.Add((ushort)(vertices.Count - 2 - (i + 1) % tessellation));
            }

            for (int i = 0; i < tessellation / 2 - 2; i++)
            {
                for (int j = 0; j < tessellation; j++)
                {
                    int nextColumn = (j + 1) % tessellation;

                    indices.Add((ushort)(i * tessellation + nextColumn + 1));
                    indices.Add((ushort)(i * tessellation + j + 1));
                    indices.Add((ushort)((i + 1) * tessellation + j + 1));

                    indices.Add((ushort)((i + 1) * tessellation + nextColumn + 1));
                    indices.Add((ushort)(i * tessellation + nextColumn + 1));
                    indices.Add((ushort)((i + 1) * tessellation + j + 1));
                }
            }

            for (int i = 0; i < tessellation; i++)
            {
                indices.Add(0);
                indices.Add((ushort)(i + 1));
                indices.Add((ushort)((i + 1) % tessellation + 1));
            }

            return new SimpleMesh(vertices.ToArray(), normals.ToArray(), textures.ToArray(), indices.ToArray());
        }

        public static SimpleMesh CreateCylinder() { return CreateCylinder(1f, 0.5f); }
        public static SimpleMesh CreateCylinder(float height, float radius) { return CreateCylinder(height, radius, DefaultTessellation); }
        public static SimpleMesh CreateCylinder(float height, float radius, int tessellation)
        {
            float verticalOffset = height / 2;
            float angleBetweenFacets = MathHelper.TwoPi / tessellation;

            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var textures = new List<Vector2>();
            var indices = new List<ushort>();

            //Create the vertex list
            for (int i = 0; i < tessellation; i++)
            {
                float theta = i * angleBetweenFacets;
                float x = (float)Math.Cos(theta) * radius;
                float z = (float)Math.Sin(theta) * radius;
                //Top cap
                vertices.Add(new Vector3(x, verticalOffset, z)); normals.Add(Vector3.Up); textures.Add(new Vector2(i / (float)(tessellation - 1), 1));
                //Top part of body
                vertices.Add(new Vector3(x, verticalOffset, z)); normals.Add(new Vector3(x, 0, z)); textures.Add(new Vector2(i / (float)(tessellation - 1), 1));
                //Bottom part of body
                vertices.Add(new Vector3(x, -verticalOffset, z)); normals.Add(new Vector3(x, 0, z)); textures.Add(new Vector2(i / (float)(tessellation - 1), 0));
                //Bottom cap
                vertices.Add(new Vector3(x, -verticalOffset, z)); normals.Add(Vector3.Down); textures.Add(new Vector2(i / (float)(tessellation - 1), 0));
            }

            //Create the index list
            //The vertices are arranged a little nonintuitively.
            //0 is part of the top cap, 1 is the upper body, 2 is lower body, and 3 is bottom cap.
            for (ushort i = 0; i < vertices.Count; i += 4)
            {
                //Each iteration, the loop advances to the next vertex 'column.'
                //Four triangles per column (except for the four degenerate cap triangles).

                //Top cap triangles
                var nextIndex = (ushort)((i + 4) % vertices.Count);
                if (nextIndex != 0) //Don't add cap indices if it's going to be a degenerate triangle.
                {
                    indices.Add(i);
                    indices.Add(nextIndex);
                    indices.Add(0);
                }

                //Body triangles
                nextIndex = (ushort)((i + 5) % vertices.Count);
                indices.Add((ushort)(i + 1));
                indices.Add((ushort)(i + 2));
                indices.Add(nextIndex);

                indices.Add(nextIndex);
                indices.Add((ushort)(i + 2));
                indices.Add((ushort)((i + 6) % vertices.Count));

                //Bottom cap triangles.
                nextIndex = (ushort)((i + 7) % vertices.Count);
                if (nextIndex != 3) //Don't add cap indices if it's going to be a degenerate triangle.
                {
                    indices.Add((ushort)(i + 3));
                    indices.Add(3);
                    indices.Add(nextIndex);
                }
            }
            return new SimpleMesh(vertices.ToArray(), normals.ToArray(), textures.ToArray(), indices.ToArray());
        }

        public static SimpleMesh CreateCone() { return CreateCone(1f, 0.5f); }
        public static SimpleMesh CreateCone(float height, float radius) { return CreateCone(height, radius, DefaultTessellation); }
        public static SimpleMesh CreateCone(float height, float radius, int tessellation)
        {
            float verticalOffset = -height / 4;
            float angleBetweenFacets = MathHelper.TwoPi / tessellation;

            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var textures = new List<Vector2>();
            var indices = new List<ushort>();

            //Create the vertex list
            var topVertexPosition = new Vector3(0, height + verticalOffset, 0);

            for (int i = 0; i < tessellation; i++)
            {
                float theta = i * angleBetweenFacets;
                var position = new Vector3((float)Math.Cos(theta) * radius, verticalOffset, (float)Math.Sin(theta) * radius);
                var offset = topVertexPosition - position;
                var normal = Vector3.Normalize(Vector3.Cross(Vector3.Cross(offset, Vector3.Up), offset));
                //Top vertex
                vertices.Add(topVertexPosition); normals.Add(normal); textures.Add(new Vector2(i / (float)(tessellation - 1), 1));
                //Sloped vertices
                vertices.Add(position); normals.Add(normal); textures.Add(new Vector2(i / (float)(tessellation - 1), 0));
                //Bottom vertices
                vertices.Add(position); normals.Add(Vector3.Down); textures.Add(new Vector2(i / (float)(tessellation - 1), 0));
            }

            //Create the index list
            for (ushort i = 0; i < vertices.Count; i += 3)
            {
                //Each iteration, the loop advances to the next vertex 'column.'
                //Four triangles per column (except for the four degenerate cap triangles).

                //Sloped Triangles
                indices.Add(i);
                indices.Add((ushort)(i + 1));
                indices.Add((ushort)((i + 4) % vertices.Count));

                //Bottom cap triangles.
                var nextIndex = (ushort)((i + 5) % vertices.Count);
                if (nextIndex != 2) //Don't add cap indices if it's going to be a degenerate triangle.
                {
                    indices.Add((ushort)(i + 2));
                    indices.Add(2);
                    indices.Add(nextIndex);
                }
            }

            return new SimpleMesh(vertices.ToArray(), normals.ToArray(), textures.ToArray(), indices.ToArray());
        }

        public static SimpleMesh CreateCapsule() { return CreateCapsule(1f, 0.5f); }
        public static SimpleMesh CreateCapsule(float height, float radius) { return CreateCapsule(height, radius, DefaultTessellation); }
        public static SimpleMesh CreateCapsule(float height, float radius, int tessellation)
        {
            var n = new Vector3();
            var offset = new Vector3(0, height / 2f, 0);
            float angleBetweenFacets = MathHelper.TwoPi / tessellation;

            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var textures = new List<Vector2>();
            var indices = new List<ushort>();

            //Create the vertex list

            //Top
            vertices.Add(new Vector3(0, radius + height / 2, 0)); normals.Add(Vector3.Up); textures.Add(new Vector2(0, 1));
            //Upper hemisphere
            for (int i = 1; i <= tessellation / 4; i++)
            {
                float phi = MathHelper.PiOver2 - i * angleBetweenFacets;
                float sinPhi = (float)Math.Sin(phi);
                float cosPhi = (float)Math.Cos(phi);

                for (int j = 0; j < tessellation; j++)
                {
                    float theta = j * angleBetweenFacets;

                    n.X = (float)Math.Cos(theta) * cosPhi;
                    n.Y = sinPhi;
                    n.Z = (float)Math.Sin(theta) * cosPhi;

                    vertices.Add(n * radius + offset); normals.Add(n); textures.Add(new Vector2(j / (float)(tessellation - 1), sinPhi));
                }
            }

            //Lower hemisphere
            for (int i = tessellation / 4; i < tessellation / 2; i++)
            {
                float phi = MathHelper.PiOver2 - i * angleBetweenFacets;
                float sinPhi = (float)Math.Sin(phi);
                float cosPhi = (float)Math.Cos(phi);

                for (int j = 0; j < tessellation; j++)
                {
                    float theta = j * angleBetweenFacets;

                    n.X = (float)Math.Cos(theta) * cosPhi;
                    n.Y = sinPhi;
                    n.Z = (float)Math.Sin(theta) * cosPhi;

                    vertices.Add(n * radius - offset); normals.Add(n); textures.Add(new Vector2(j / (float)(tessellation - 1), cosPhi));
                }
            }

            //Bottom
            vertices.Add(new Vector3(0, -radius - height / 2, 0)); normals.Add(Vector3.Down); textures.Add(new Vector2(0, 1));

            //Create the index list
            for (int i = 0; i < tessellation; i++)
            {
                indices.Add((ushort)(vertices.Count - 1));
                indices.Add((ushort)(vertices.Count - 2 - i));
                indices.Add((ushort)(vertices.Count - 2 - (i + 1) % tessellation));
            }

            for (int i = 0; i < tessellation / 2 - 1; i++)
            {
                for (int j = 0; j < tessellation; j++)
                {
                    int nextColumn = (j + 1) % tessellation;

                    indices.Add((ushort)(i * tessellation + nextColumn + 1));
                    indices.Add((ushort)(i * tessellation + j + 1));
                    indices.Add((ushort)((i + 1) * tessellation + j + 1));

                    indices.Add((ushort)((i + 1) * tessellation + nextColumn + 1));
                    indices.Add((ushort)(i * tessellation + nextColumn + 1));
                    indices.Add((ushort)((i + 1) * tessellation + j + 1));
                }
            }

            for (int i = 0; i < tessellation; i++)
            {
                indices.Add(0);
                indices.Add((ushort)(i + 1));
                indices.Add((ushort)((i + 1) % tessellation + 1));
            }

            return new SimpleMesh(vertices.ToArray(), normals.ToArray(), textures.ToArray(), indices.ToArray());
        }
    }
}
