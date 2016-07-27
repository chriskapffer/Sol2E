using System;
using Microsoft.Xna.Framework;
using Sol2E.Core;

namespace Sol2E.Graphics
{
    /// <summary>
    /// A Component, which marks a camera entity and contains fields like
    /// FieldOfView, AspectRatio, NearPlaneDistance and FarPlaneDistance.
    /// </summary>
    [Serializable]
    public class Camera : Component
    {
        // static instance for faster look up
        public static Camera ActiveCamera { get; private set; }

        // makes this listener instance active or not
        public bool IsActive
        {
            get
            {
                return this == ActiveCamera;
            }
            set
            {
                if (value && this != ActiveCamera)
                    ActiveCamera = this;
                else if (!value && this == ActiveCamera)
                    ActiveCamera = null;
            }
        }

        public Camera()
        {
            if (ActiveCamera == null)
                ActiveCamera = this;

            FieldOfView = MathHelper.ToRadians(45);
            AspectRatio = 1.66666667f;
            NearPlaneDistance = 0.05f;
            FarPlaneDistance = 100;
        }

        private float _fieldOfView;
        public float FieldOfView
        {
            get
            {
                return _fieldOfView;
            }
            set
            {
                if (Math.Abs(value - _fieldOfView) > float.Epsilon)
                {
                    _fieldOfView = value;
                    ComponentChangedEvent<Camera>.Invoke(this, "FieldOfView");
                }
            }
        }

        private float _aspectRatio;
        public float AspectRatio
        {
            get
            {
                return _aspectRatio;
            }
            set
            {
                if (Math.Abs(value - _aspectRatio) > float.Epsilon)
                {
                    _aspectRatio = value;
                    ComponentChangedEvent<Camera>.Invoke(this, "AspectRatio");
                }
            }
        }

        private float _nearPlaneDistance;
        public float NearPlaneDistance
        {
            get
            {
                return _nearPlaneDistance;
            }
            set
            {
                if (Math.Abs(value - _nearPlaneDistance) > float.Epsilon)
                {
                    _nearPlaneDistance = value;
                    ComponentChangedEvent<Camera>.Invoke(this, "NearPlaneDistance");
                }
            }
        }

        private float _farPlaneDistance;
        public float FarPlaneDistance
        {
            get
            {
                return _farPlaneDistance;
            }
            set
            {
                if (Math.Abs(value - _farPlaneDistance) > float.Epsilon)
                {
                    _farPlaneDistance = value;
                    ComponentChangedEvent<Camera>.Invoke(this, "FarPlaneDistance");
                }
            }
        }

        // returns constructed ProjectionMatrix
        public Matrix Projection
        {
            get
            {
                return Matrix.CreatePerspectiveFieldOfView(
                    FieldOfView, AspectRatio, NearPlaneDistance, FarPlaneDistance);
            }
        }
    }
}
