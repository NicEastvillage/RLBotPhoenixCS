using System;
using rlbot.flat;

namespace RedUtils.Math
{
	/// <summary>Represents a 3x3 matrix, meant to represent orientaion</summary>
	public struct Mat3x3
	{
		/// <summary>A normalized vector pointing forward, relative to the orientation</summary>
		public Vec3 Forward { get; private set; }
		/// <summary>A normalized vector pointing right, relative to the orientation</summary>
		public Vec3 Right { get; private set; }
		/// <summary>A normalized vector pointing up, relative to the orientation</summary>
		public Vec3 Up { get; private set; }

		/// <summary>Initializes a matrix with three vectors</summary>
		public Mat3x3(Vec3 forward, Vec3 right, Vec3 up)
		{
			Forward = forward;
			Right = right;
			Up = up;
		}

		/// <summary>Initializes a matrix using pitch, yaw and roll (stored in a vector)</summary>
		public Mat3x3(Vec3 rotation)
		{
			float cosPitch = MathF.Cos(rotation[0]);
			float sinPitch = MathF.Sin(rotation[0]);
			float cosYaw = MathF.Cos(rotation[1]);
			float sinYaw = MathF.Sin(rotation[1]);
			float cosRoll = MathF.Cos(rotation[2]);
			float sinRoll = MathF.Sin(rotation[2]);
			Forward = new Vec3(cosPitch * cosYaw, cosPitch * sinYaw, sinPitch);
			Right = new Vec3(cosYaw*sinPitch*sinRoll-cosRoll*sinYaw, sinYaw*sinPitch*sinRoll+cosRoll*cosYaw, -cosPitch*sinRoll);
			Up = new Vec3(-cosRoll*cosYaw*sinPitch-sinRoll*sinYaw, -cosRoll*sinYaw*sinPitch+sinRoll*cosYaw, cosPitch*cosRoll);
		}

		/// <summary>Returns the dot product between this matrix and the given vector</summary>
		public Vec3 Dot(Vec3 v)
		{
			return new Vec3(Forward.Dot(v), Right.Dot(v), Up.Dot(v));
		}

		/// <summary>Returns the transpose of this matrix</summary>
		public Mat3x3 Transpose()
		{
			return new Mat3x3(
				new Vec3(Forward.x, Right.x, Up.x),
				new Vec3(Forward.y, Right.y, Up.y),
				new Vec3(Forward.z, Right.z, Up.z)
			);
		}

		/// <summary>Returns the rotation matrix that will rotate its target around the given axis by the given angle</summary>
		public static Mat3x3 RotationFromAxis(Vec3 axis, float angle)
		{
			float cos = MathF.Cos(angle);
			float sin = MathF.Sin(angle);
			float n1Cos = 1F - cos;
			Vec3 u = axis.Normalize();
			return new Mat3x3(
				new Vec3(
					cos + u.x * u.x * n1Cos, 
					u.x * u.y * n1Cos - u.z * sin,
					u.x * u.z * n1Cos + u.y * sin),
				new Vec3(
					u.y * u.x * n1Cos + u.z * sin,
					cos + u.y * u.y * n1Cos,
					u.y * u.z * n1Cos - u.x * sin),
				new Vec3(
					u.z * u.x * n1Cos - u.y * sin,
					u.z * u.y * n1Cos + u.x * sin,
					cos + u.z * u.z * n1Cos)
			);
		}
	}
}
