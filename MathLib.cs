using System;
using SharpDX;

namespace SoftRender.Net
{
    public class MathLib
    {
        public static float Clamp(float value, float min = 0f, float max = 1f)
        {
            return Math.Max(min, Math.Min(value, max));
        }

        public static float Interpolate(float min, float max, float gradient)
        {
            return min + (max - min) * Clamp(gradient);
        }

        public static float ComputeNDotL(Vector3 coord, Vector3 normal, Vector3 light)
        {
            var lightDirection = light - coord;
            normal.Normalize();
            lightDirection.Normalize();
            return Math.Max(0, Vector3.Dot(normal, lightDirection));
        }
    }
}
