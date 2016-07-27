using System;
using Microsoft.Xna.Framework;
using Sol2E.Core;

namespace Sol2E.Common
{
    /// <summary>
    /// Extension class for Vector3
    /// </summary>
    public static class Vector3Extensions
    {
        /// <summary>
        /// Normalizes a Vector3 instance and returns it.
        /// </summary>
        /// <param name="v">instance</param>
        /// <returns>normalized vector</returns>
        public static Vector3 Normalized(this Vector3 v)
        {
            v.Normalize();
            return v;
        }
    }

    /// <summary>
    /// A Component, which contains Position, Scale and Orientation
    /// </summary>
    [Serializable]
    public class Transform : Component
    {
        // static default instance, used if a Transform instance
        // is necessary but not existent in a given context
        public static Transform Default = new Transform();
        
        public Transform()
        {
            Position = Vector3.Zero;
            Scale = Vector3.One;
            Orientation = Quaternion.Identity;
        }

        #region Fields

        private Vector3 _position;
        public Vector3 Position
        {
            get
            {
                return _position;
            }
            set
            {
                if (_position != value)
                {
                    _position = value;
                    ComponentChangedEvent<Transform>.Invoke(this, "Position");
                }
            }
        }

        private Vector3 _scale;
        public Vector3 Scale
        {
            get
            {
                return _scale;
            }
            set
            {
                if (_scale != value)
                {
                    _scale = value;
                    ComponentChangedEvent<Transform>.Invoke(this, "Scale");
                }
            }
        }

        private Quaternion _orientation;
        public Quaternion Orientation
        {
            get
            {
                return _orientation;
            }
            set
            {
                if (_orientation != value)
                {
                    _orientation = value;
                    ComponentChangedEvent<Transform>.Invoke(this, "Orientation");
                }
            }
        }

        // simplifies transform accessory
        public Matrix World
        {
            get
            {
                return Matrix.CreateScale(Scale)
                    * Matrix.CreateFromQuaternion(Orientation)
                    * Matrix.CreateTranslation(Position);
            }
            set
            {
                value.Decompose(out _scale, out _orientation, out _position);
                ComponentChangedEvent<Transform>.Invoke(this, "World");
            }
        }

        public Matrix View
        {
            get { return Matrix.CreateLookAt(Position, Position + Forward, Up); }
        }

        public Vector3 Forward
        {
            get { return Vector3.Transform(Vector3.Forward, Orientation); }
        }
        public Vector3 Up
        {
            get { return Vector3.Transform(Vector3.Up, Orientation); }
        }
        public Vector3 Right
        {
            get { return Vector3.Transform(Vector3.Right, Orientation); }
        }
        public Vector3 Backward
        {
            get { return Vector3.Transform(Vector3.Backward, Orientation); }
        }
        public Vector3 Down
        {
            get { return Vector3.Transform(Vector3.Down, Orientation); }
        }
        public Vector3 Left
        {
            get { return Vector3.Transform(Vector3.Left, Orientation); }
        }

        #endregion

        #region Orientation Methods

        public void RotateGlobalX(float degrees, bool avoidUnintentionalRoll = false)
        {
            RotateGlobalAxisAngle(Vector3.UnitX, degrees, avoidUnintentionalRoll);
        }
        public void RotateGlobalY(float degrees, bool avoidUnintentionalRoll = false)
        {
            RotateGlobalAxisAngle(Vector3.UnitY, degrees, avoidUnintentionalRoll);
        }
        public void RotateGlobalZ(float degrees, bool avoidUnintentionalRoll = false)
        {
            RotateGlobalAxisAngle(Vector3.UnitZ, degrees, avoidUnintentionalRoll);
        }

        public void RotateLocalX(float degrees, bool avoidUnintentionalRoll = false)
        {
            RotateGlobalAxisAngle(Right, degrees, avoidUnintentionalRoll);
        }
        public void RotateLocalY(float degrees, bool avoidUnintentionalRoll = false)
        {
            RotateGlobalAxisAngle(Up, degrees, avoidUnintentionalRoll);
        }
        public void RotateLocalZ(float degrees, bool avoidUnintentionalRoll = false)
        {
            RotateGlobalAxisAngle(Forward, degrees, avoidUnintentionalRoll);
        }

        public void RotateLocalAxisAngle(Vector3 axis, float degrees, bool avoidUnintentionalRoll = false)
        {
            RotateGlobalAxisAngle(Vector3.Transform(axis, Orientation), degrees, avoidUnintentionalRoll);
        }
        public void RotateGlobalAxisAngle(Vector3 axis, float degrees, bool avoidUnintentionalRoll = false)
        {
            if (avoidUnintentionalRoll)
            {
                RotateGlobalAxisAngleByAvoidingRoll(axis, degrees);
            }
            else
            {
                Quaternion rotation = Quaternion.CreateFromAxisAngle(axis, MathHelper.ToRadians(degrees));
                Orientation = Quaternion.Concatenate(Orientation, rotation);
            }
        }
        public void RotateGlobalAxisAngleByAvoidingRoll(Vector3 axis, float degrees)
        {
            float yaw, pitch, rollOld, rollNew;
            YawPitchRollOfQuaternion(Orientation, out yaw, out pitch, out rollOld);

            RotateGlobalAxisAngle(axis, degrees);

            YawPitchRollOfQuaternion(Orientation, out yaw, out pitch, out rollNew);
            if (Math.Abs(rollOld - rollNew) > float.Epsilon)
            {
                Orientation = Quaternion.CreateFromYawPitchRoll(yaw, pitch, rollOld);
            }
        }

        public void RotateToPoint(Vector3 point)
        {
            RotateToPointWithUpVector(point, Vector3.UnitY);
        }

        public void RotateToPointWithUpVector(Vector3 point, Vector3 up)
        {
            Matrix rotation = Matrix.Invert(Matrix.CreateLookAt(Position, point, up));
            Orientation = Quaternion.CreateFromRotationMatrix(rotation);
        }

        // TODO: implementation
        //public void RotateToPointDirectly(Vector3 point)
        //{
        //    Vector3 direction = Vector3.Normalize(point - Position);
        //    Vector3 axis = Vector3.Normalize(Vector3.Cross(Forward, direction));
        //    float angle = (float)Math.Acos(Vector3.Dot(Forward, direction));
        //    Quaternion rotation = Quaternion.CreateFromAxisAngle(axis, angle);
        //    Orientation = Quaternion.Concatenate(Orientation, rotation);
        //}

        public void ResetOrientation()
        {
            Orientation = Quaternion.Identity;
        }

        #endregion

        /// <summary>
        /// Retrieves yaw, pitch and roll from a given quaternion
        /// </summary>
        /// <param name="q">quaternion to retriev yaw, pitch and roll from</param>
        /// <param name="yaw">yaw</param>
        /// <param name="pitch">pitch</param>
        /// <param name="roll">roll</param>
        private void YawPitchRollOfQuaternion(Quaternion q, out float yaw, out float pitch, out float roll)
        {
            const float threshold = 0.5f - float.Epsilon;

            float XY = q.X * q.Y;
            float ZW = q.Z * q.W;
            float test = XY + ZW;


            if (test < -threshold || test > threshold)
            {
                int sign = Math.Sign(test);

                yaw = sign * 2 * (float)Math.Atan2(q.X, q.W);
                pitch = sign * MathHelper.PiOver2;
                roll = 0;
            }
            else
            {
                float XX = q.X * q.X;
                float XZ = q.X * q.Z;
                float XW = q.X * q.W;

                float YY = q.Y * q.Y;
                float YW = q.Y * q.W;
                float YZ = q.Y * q.Z;

                float ZZ = q.Z * q.Z;

                yaw = (float)Math.Atan2(2 * YW - 2 * XZ, 1 - 2 * YY - 2 * ZZ);
                pitch = (float)Math.Atan2(2 * XW - 2 * YZ, 1 - 2 * XX - 2 * ZZ);
                roll = (float)Math.Asin(2 * test);
            }
        }
    }
}
