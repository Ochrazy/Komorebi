using System;
using OpenTK;

namespace Komorebi
{
	static public class Utility
	{
		static public Matrix4 getNormalMatrix(Matrix4 modelMatrix, Matrix4 viewMatrix)
		{
			return Matrix4.Transpose(Matrix4.Invert(modelMatrix * viewMatrix));
		}
			
		static public Matrix4 getNormalMatrix(Matrix4 modelViewMatrix)
		{
			return Matrix4.Transpose(Matrix4.Invert(modelViewMatrix));
		}

		// Transform a point in 3D to a point on the screen in 2D (with depth)
		static public Vector3 transformWorldPositionToScreenSpace(Vector3 pos, Matrix4 viewMatrix, Matrix4 projectionMatrix)
		{
			pos = Vector3.Transform(pos, viewMatrix);
			pos = Vector3.Transform(pos, projectionMatrix);
			pos.X /= pos.Z;
			pos.Y /= pos.Z;
			pos.X = (pos.X + 1) / 2;
			pos.Y = (pos.Y + 1) / 2;

			return pos;
		}

        public static float degreesToRadians(float degrees)
        {
            const float degToRad = (float)System.Math.PI / 180.0f;
            return degrees * degToRad;
        }

        public static float radiansToDegrees(float radians)
        {
            const float radToDeg = 180.0f / (float)System.Math.PI;
            return radians * radToDeg;
        }
    }
}

